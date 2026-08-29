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
    public class ClusterFeatureCategoryControllerTests
    {
        private SqliteConnection? _masterConnection;
        private SqlConnectionManager? _sqlConnectionManager;
        private ILogger<SqlConnectionManager>? _sqlLogger;
        private ILogger<ClusterFeatureCategoryManager>? _featureLogger;
        private ClusterFeatureCategoryController? _controller;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _masterConnection = new SqliteConnection("Data Source=ClusterFeatureCategoryTest;Mode=Memory;Cache=Shared");
            _masterConnection.Open();

            _sqlLogger = NullLogger<SqlConnectionManager>.Instance;
            _featureLogger = NullLogger<ClusterFeatureCategoryManager>.Instance;
            _sqlConnectionManager = new SqlConnectionManager(
                "Data Source=ClusterFeatureCategoryTest;Mode=Memory;Cache=Shared",
                _sqlLogger);
            _controller = new ClusterFeatureCategoryController(_featureLogger, _sqlConnectionManager);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _masterConnection?.Dispose();
        }

        [Test]
        public void ClusterFeatureCategory_CRUD_Works()
        {
            Guid id = Guid.NewGuid();
            ClusterFeatureCategory category = new()
            {
                MetaInfo = new MetaInfo { ID = id },
                Name = "Test cluster feature",
                IsExclusive = true,
                HasValidityPeriod = true,
                Options =
                [
                    new ClusterFeatureOption { ID = Guid.NewGuid(), Name = "Option A" },
                    new ClusterFeatureOption { ID = Guid.NewGuid(), Name = "Option B" }
                ]
            };

            Assert.That(_controller!.PostClusterFeatureCategory(category), Is.TypeOf<OkResult>());

            ActionResult<ClusterFeatureCategory?> fetchedResult = _controller.GetClusterFeatureCategoryById(id);
            Assert.That(fetchedResult.Result, Is.TypeOf<OkObjectResult>());
            ClusterFeatureCategory? fetched = ((OkObjectResult)fetchedResult.Result!).Value as ClusterFeatureCategory;
            Assert.That(fetched, Is.Not.Null);
            Assert.That(fetched!.Name, Is.EqualTo("Test cluster feature"));
            Assert.That(fetched.Options, Has.Count.EqualTo(2));

            fetched.Name = "Updated cluster feature";
            Assert.That(_controller.PutClusterFeatureCategoryById(id, fetched), Is.TypeOf<OkResult>());

            ActionResult<ClusterFeatureCategory?> updatedResult = _controller.GetClusterFeatureCategoryById(id);
            ClusterFeatureCategory? updated = ((OkObjectResult)updatedResult.Result!).Value as ClusterFeatureCategory;
            Assert.That(updated?.Name, Is.EqualTo("Updated cluster feature"));

            Assert.That(_controller.DeleteClusterFeatureCategoryById(id), Is.TypeOf<OkResult>());
            Assert.That(_controller.GetClusterFeatureCategoryById(id).Result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public void ClusterFeatureCategory_ListEndpoints_ReturnOk()
        {
            Assert.That(_controller!.GetAllClusterFeatureCategoryId().Result, Is.TypeOf<OkObjectResult>());
            Assert.That(((OkObjectResult)_controller.GetAllClusterFeatureCategoryId().Result!).Value, Is.InstanceOf<IEnumerable<Guid>>());

            Assert.That(_controller.GetAllClusterFeatureCategoryMetaInfo().Result, Is.TypeOf<OkObjectResult>());
            Assert.That(_controller.GetAllClusterFeatureCategory().Result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public void ClusterFeatureCategory_BadRequest_OnInvalidBody()
        {
            Assert.That(_controller!.PostClusterFeatureCategory(null), Is.TypeOf<BadRequestResult>());
            Assert.That(_controller.PostClusterFeatureCategory(new ClusterFeatureCategory { MetaInfo = null }), Is.TypeOf<BadRequestResult>());
            Assert.That(_controller.PostClusterFeatureCategory(new ClusterFeatureCategory { MetaInfo = new MetaInfo { ID = Guid.Empty } }), Is.TypeOf<BadRequestResult>());
        }
    }
}
