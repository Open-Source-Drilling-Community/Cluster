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
    public class SlotFeatureCategoryControllerTests
    {
        private SqliteConnection? _masterConnection;
        private SqlConnectionManager? _sqlConnectionManager;
        private ILogger<SqlConnectionManager>? _sqlLogger;
        private ILogger<SlotFeatureCategoryManager>? _featureLogger;
        private SlotFeatureCategoryController? _controller;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _masterConnection = new SqliteConnection("Data Source=SlotFeatureCategoryTest;Mode=Memory;Cache=Shared");
            _masterConnection.Open();

            _sqlLogger = NullLogger<SqlConnectionManager>.Instance;
            _featureLogger = NullLogger<SlotFeatureCategoryManager>.Instance;
            _sqlConnectionManager = new SqlConnectionManager(
                "Data Source=SlotFeatureCategoryTest;Mode=Memory;Cache=Shared",
                _sqlLogger);
            _controller = new SlotFeatureCategoryController(_featureLogger, _sqlConnectionManager);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _masterConnection?.Dispose();
        }

        [Test]
        public void SlotFeatureCategory_CRUD_Works()
        {
            Guid id = Guid.NewGuid();
            SlotFeatureCategory category = new()
            {
                MetaInfo = new MetaInfo { ID = id },
                Name = "Test slot feature",
                IsExclusive = true,
                HasValidityPeriod = true,
                Options =
                [
                    new SlotFeatureOption { ID = Guid.NewGuid(), Name = "Option A" },
                    new SlotFeatureOption { ID = Guid.NewGuid(), Name = "Option B" }
                ]
            };

            Assert.That(_controller!.PostSlotFeatureCategory(category), Is.TypeOf<OkResult>());

            ActionResult<SlotFeatureCategory?> fetchedResult = _controller.GetSlotFeatureCategoryById(id);
            Assert.That(fetchedResult.Result, Is.TypeOf<OkObjectResult>());
            SlotFeatureCategory? fetched = ((OkObjectResult)fetchedResult.Result!).Value as SlotFeatureCategory;
            Assert.That(fetched, Is.Not.Null);
            Assert.That(fetched!.Name, Is.EqualTo("Test slot feature"));
            Assert.That(fetched.Options, Has.Count.EqualTo(2));

            fetched.Name = "Updated slot feature";
            Assert.That(_controller.PutSlotFeatureCategoryById(id, fetched), Is.TypeOf<OkResult>());

            ActionResult<SlotFeatureCategory?> updatedResult = _controller.GetSlotFeatureCategoryById(id);
            SlotFeatureCategory? updated = ((OkObjectResult)updatedResult.Result!).Value as SlotFeatureCategory;
            Assert.That(updated?.Name, Is.EqualTo("Updated slot feature"));

            Assert.That(_controller.DeleteSlotFeatureCategoryById(id), Is.TypeOf<OkResult>());
            Assert.That(_controller.GetSlotFeatureCategoryById(id).Result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public void SlotFeatureCategory_ListEndpoints_ReturnOk()
        {
            Assert.That(_controller!.GetAllSlotFeatureCategoryId().Result, Is.TypeOf<OkObjectResult>());
            Assert.That(((OkObjectResult)_controller.GetAllSlotFeatureCategoryId().Result!).Value, Is.InstanceOf<IEnumerable<Guid>>());

            Assert.That(_controller.GetAllSlotFeatureCategoryMetaInfo().Result, Is.TypeOf<OkObjectResult>());
            Assert.That(_controller.GetAllSlotFeatureCategory().Result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public void SlotFeatureCategory_BadRequest_OnInvalidBody()
        {
            Assert.That(_controller!.PostSlotFeatureCategory(null), Is.TypeOf<BadRequestResult>());
            Assert.That(_controller.PostSlotFeatureCategory(new SlotFeatureCategory { MetaInfo = null }), Is.TypeOf<BadRequestResult>());
            Assert.That(_controller.PostSlotFeatureCategory(new SlotFeatureCategory { MetaInfo = new MetaInfo { ID = Guid.Empty } }), Is.TypeOf<BadRequestResult>());
        }
    }
}

