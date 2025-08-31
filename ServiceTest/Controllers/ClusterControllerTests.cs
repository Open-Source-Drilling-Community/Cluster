using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using NORCE.Drilling.Cluster.Service.Controllers;
using NORCE.Drilling.Cluster.Service.Managers;
using OSDC.DotnetLibraries.General.DataManagement;

namespace ServiceTest.Controllers
{
    [TestFixture]
    public class ClusterControllerTests
    {
        private SqliteConnection? _masterConnection;
        private SqlConnectionManager? _sqlConnectionManager;
        private ILogger<SqlConnectionManager>? _sqlLogger;
        private ILogger<ClusterManager>? _clusterLogger;
        private ClusterController? _controller;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Keep a master connection open to persist the in-memory DB across connections
            _masterConnection = new SqliteConnection("Data Source=ClusterTest;Mode=Memory;Cache=Shared");
            _masterConnection.Open();

            _sqlLogger = NullLogger<SqlConnectionManager>.Instance;
            _clusterLogger = NullLogger<ClusterManager>.Instance;

            _sqlConnectionManager = new SqlConnectionManager(
                "Data Source=ClusterTest;Mode=Memory;Cache=Shared",
                _sqlLogger);

            // Instantiate controller normally; it will pick up the singleton ClusterManager
            _controller = new ClusterController(_clusterLogger, _sqlConnectionManager);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _masterConnection?.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            // Ensure DB is clean before each test
            var manager = ClusterManager.GetInstance(_clusterLogger!, _sqlConnectionManager!);
            manager.Clear();
        }

        private static NORCE.Drilling.Cluster.Model.Cluster MakeCluster(Guid id)
        {
            return new NORCE.Drilling.Cluster.Model.Cluster
            {
                MetaInfo = new MetaInfo { ID = id },
                Name = "Test Cluster",
                Description = "",
                FieldID = null,
                IsSingleWell = false
            };
        }

        [Test]
        public void GetAllClusterId_Empty_ReturnsOkWithEmptyList()
        {
            var result = _controller!.GetAllClusterId();
            Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
            var ok = (OkObjectResult)result.Result!;
            Assert.That(ok.Value, Is.InstanceOf<IEnumerable<Guid>>());
            Assert.That(((IEnumerable<Guid>)ok.Value!).GetEnumerator().MoveNext(), Is.False);
        }

        [Test]
        public void PostCluster_NewItem_ReturnsOkAndPersists()
        {
            var id = Guid.NewGuid();
            var cluster = MakeCluster(id);

            var postRes = _controller!.PostCluster(cluster);
            Assert.That(postRes, Is.TypeOf<OkResult>());

            var getById = _controller!.GetClusterById(id);
            Assert.That(getById.Result, Is.TypeOf<OkObjectResult>());
            var ok = (OkObjectResult)getById.Result!;
            Assert.That(ok.Value, Is.Not.Null);
            var returned = ok.Value as NORCE.Drilling.Cluster.Model.Cluster;
            Assert.That(returned, Is.Not.Null);
            Assert.That(returned!.MetaInfo, Is.Not.Null);
            Assert.That(returned!.MetaInfo!.ID, Is.EqualTo(id));
        }

        [Test]
        public void PostCluster_Duplicate_ReturnsConflict()
        {
            var id = Guid.NewGuid();
            var cluster = MakeCluster(id);
            var cluster2 = MakeCluster(id);

            var r1 = _controller!.PostCluster(cluster);
            Assert.That(r1, Is.TypeOf<OkResult>());

            var r2 = _controller!.PostCluster(cluster2);
            Assert.That(r2, Is.TypeOf<StatusCodeResult>());
            var obj = (StatusCodeResult)r2;
            Assert.That(obj.StatusCode, Is.EqualTo(409));
        }

        [Test]
        public void PostCluster_BadRequestOnNullOrInvalid()
        {
            var r1 = _controller!.PostCluster(null);
            Assert.That(r1, Is.TypeOf<BadRequestResult>());

            var r2 = _controller!.PostCluster(new NORCE.Drilling.Cluster.Model.Cluster { MetaInfo = null });
            Assert.That(r2, Is.TypeOf<BadRequestResult>());

            var r3 = _controller!.PostCluster(new NORCE.Drilling.Cluster.Model.Cluster { MetaInfo = new MetaInfo { ID = Guid.Empty } });
            Assert.That(r3, Is.TypeOf<BadRequestResult>());
        }

        [Test]
        public void GetClusterById_EmptyGuid_BadRequest()
        {
            var res = _controller!.GetClusterById(Guid.Empty);
            Assert.That(res.Result, Is.TypeOf<BadRequestResult>());
        }

        [Test]
        public void GetClusterById_NotFound()
        {
            var res = _controller!.GetClusterById(Guid.NewGuid());
            Assert.That(res.Result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public void GetAllCluster_ReturnsOkWithItems()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            _controller!.PostCluster(MakeCluster(id1));
            _controller!.PostCluster(MakeCluster(id2));

            var res = _controller!.GetAllCluster();
            Assert.That(res.Result, Is.TypeOf<OkObjectResult>());
            var ok = (OkObjectResult)res.Result!;
            Assert.That(ok.Value, Is.InstanceOf<IEnumerable<NORCE.Drilling.Cluster.Model.Cluster?>>());
            var list = (IEnumerable<NORCE.Drilling.Cluster.Model.Cluster?>)ok.Value!;
            Assert.That(list, Is.Not.Null);
        }

        [Test]
        public void PutClusterById_NotFound()
        {
            var id = Guid.NewGuid();
            var cluster = MakeCluster(id);
            var res = _controller!.PutClusterById(id, cluster);
            // Not found because item not yet created
            Assert.That(res, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public void PutClusterById_BadRequest_OnIdMismatch()
        {
            var id = Guid.NewGuid();
            var other = Guid.NewGuid();
            var cluster = MakeCluster(other);
            var res = _controller!.PutClusterById(id, cluster);
            Assert.That(res, Is.TypeOf<BadRequestResult>());
        }

        [Test]
        public void PutClusterById_UpdatesAndReturnsOk()
        {
            var id = Guid.NewGuid();
            var cluster = MakeCluster(id);
            var post = _controller!.PostCluster(cluster);
            Assert.That(post, Is.TypeOf<OkResult>());

            // Update some non-key data
            cluster.Description = "updated";
            var res = _controller!.PutClusterById(id, cluster);
            Assert.That(res, Is.TypeOf<OkResult>());
        }

        [Test]
        public void DeleteClusterById_NotFound_ThenOk()
        {
            var id = Guid.NewGuid();
            var nf = _controller!.DeleteClusterById(id);
            Assert.That(nf, Is.TypeOf<NotFoundResult>());

            _controller!.PostCluster(MakeCluster(id));
            var ok = _controller!.DeleteClusterById(id);
            Assert.That(ok, Is.TypeOf<OkResult>());
        }
    }
}
