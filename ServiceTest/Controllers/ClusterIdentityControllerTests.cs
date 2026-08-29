using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OSDC.Drilling.Cluster.Model;
using OSDC.Drilling.Cluster.Service.Controllers;
using OSDC.Drilling.Cluster.Service.Managers;
using NUnit.Framework;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;

namespace ServiceTest.Controllers
{
    [TestFixture]
    public class ClusterIdentityControllerTests
    {
        private SqliteConnection? _masterConnection;
        private SqlConnectionManager? _sqlConnectionManager;
        private ILogger<SqlConnectionManager>? _sqlLogger;
        private ILogger<ClusterIdentityManager>? _identityLogger;
        private ClusterIdentityController? _controller;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _masterConnection = new SqliteConnection("Data Source=ClusterIdentityTest;Mode=Memory;Cache=Shared");
            _masterConnection.Open();

            _sqlLogger = NullLogger<SqlConnectionManager>.Instance;
            _identityLogger = NullLogger<ClusterIdentityManager>.Instance;
            _sqlConnectionManager = new SqlConnectionManager(
                "Data Source=ClusterIdentityTest;Mode=Memory;Cache=Shared",
                _sqlLogger);
            _controller = new ClusterIdentityController(_identityLogger, _sqlConnectionManager);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _masterConnection?.Dispose();
        }

        [Test]
        public void ClusterIdentity_CRUD_Works()
        {
            Guid id = Guid.NewGuid();
            ClusterIdentity identity = new()
            {
                MetaInfo = new MetaInfo { ID = id },
                Name = "Test cluster identity"
            };

            Assert.That(_controller!.PostClusterIdentity(identity), Is.TypeOf<OkResult>());

            ActionResult<ClusterIdentity?> fetchedResult = _controller.GetClusterIdentityById(id);
            Assert.That(fetchedResult.Result, Is.TypeOf<OkObjectResult>());
            ClusterIdentity? fetched = ((OkObjectResult)fetchedResult.Result!).Value as ClusterIdentity;
            Assert.That(fetched, Is.Not.Null);
            Assert.That(fetched!.Name, Is.EqualTo("Test cluster identity"));

            fetched.Name = "Updated cluster identity";
            Assert.That(_controller.PutClusterIdentityById(id, fetched), Is.TypeOf<OkResult>());

            ActionResult<ClusterIdentity?> updatedResult = _controller.GetClusterIdentityById(id);
            ClusterIdentity? updated = ((OkObjectResult)updatedResult.Result!).Value as ClusterIdentity;
            Assert.That(updated?.Name, Is.EqualTo("Updated cluster identity"));

            Assert.That(_controller.DeleteClusterIdentityById(id), Is.TypeOf<OkResult>());
            Assert.That(_controller.GetClusterIdentityById(id).Result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public void ClusterIdentity_ListEndpoints_ReturnOk()
        {
            Assert.That(_controller!.GetAllClusterIdentityId().Result, Is.TypeOf<OkObjectResult>());
            Assert.That(((OkObjectResult)_controller.GetAllClusterIdentityId().Result!).Value, Is.InstanceOf<IEnumerable<Guid>>());

            Assert.That(_controller.GetAllClusterIdentityMetaInfo().Result, Is.TypeOf<OkObjectResult>());
            Assert.That(_controller.GetAllClusterIdentity().Result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public void ClusterIdentity_BadRequest_OnInvalidBody()
        {
            Assert.That(_controller!.PostClusterIdentity(null), Is.TypeOf<BadRequestResult>());
            Assert.That(_controller.PostClusterIdentity(new ClusterIdentity { MetaInfo = null }), Is.TypeOf<BadRequestResult>());
            Assert.That(_controller.PostClusterIdentity(new ClusterIdentity { MetaInfo = new MetaInfo { ID = Guid.Empty } }), Is.TypeOf<BadRequestResult>());
        }
    }
}
