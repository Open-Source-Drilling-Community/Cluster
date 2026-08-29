using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OSDC.Drilling.Cluster.Model;
using OSDC.Drilling.Cluster.Service.Controllers;
using OSDC.Drilling.Cluster.Service;
using OSDC.Drilling.Cluster.Service.Managers;
using NUnit.Framework;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Text.Json;

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
            Assert.That(_controller.PutClusterFeatureCategoryById(id, fetched.LastModificationDate!.Value, fetched), Is.TypeOf<OkObjectResult>());

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
        public void CategoryDeleteAndReferencedOptionRemoval_AreRejected()
        {
            Guid categoryId = Guid.NewGuid();
            Guid optionId = Guid.NewGuid();
            Guid clusterId = Guid.NewGuid();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            ClusterFeatureCategory category = new()
            {
                MetaInfo = new MetaInfo { ID = categoryId }, Name = "Referenced category",
                CreationDate = now, LastModificationDate = now,
                Options = [new ClusterFeatureOption { ID = optionId, Name = "Referenced option" }]
            };
            OSDC.Drilling.Cluster.Model.Cluster cluster = new()
            {
                MetaInfo = new MetaInfo { ID = clusterId },
                ClusterFeatureAssignments = [new ClusterFeatureAssignment { ID = Guid.NewGuid(), FeatureCategoryID = categoryId, FeatureOptionID = optionId }]
            };
            using (SqliteConnection connection = _sqlConnectionManager!.GetConnection()!)
            {
                using SqliteCommand categoryCommand = connection.CreateCommand();
                categoryCommand.CommandText = "INSERT INTO ClusterFeatureCategoryTable (ID, MetaInfo, Name, IsExclusive, HasValidityPeriod, CreationDate, LastModificationDate, ClusterFeatureCategory) VALUES ($id,$meta,$name,0,0,$created,$modified,$document)";
                categoryCommand.Parameters.AddWithValue("$id", categoryId.ToString());
                categoryCommand.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(category.MetaInfo, JsonSettings.Options));
                categoryCommand.Parameters.AddWithValue("$name", category.Name!);
                categoryCommand.Parameters.AddWithValue("$created", now.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
                categoryCommand.Parameters.AddWithValue("$modified", now.ToString(SqlConnectionManager.DATE_TIME_FORMAT));
                categoryCommand.Parameters.AddWithValue("$document", JsonSerializer.Serialize(category, JsonSettings.Options));
                categoryCommand.ExecuteNonQuery();
                using SqliteCommand clusterCommand = connection.CreateCommand();
                clusterCommand.CommandText = "INSERT INTO ClusterTable (ID, MetaInfo, FieldID, IsSingleWell, RigID, IsFixedPlatform, Cluster) VALUES ($id,$meta,'',0,'',0,$document)";
                clusterCommand.Parameters.AddWithValue("$id", clusterId.ToString());
                clusterCommand.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(cluster.MetaInfo, JsonSettings.Options));
                clusterCommand.Parameters.AddWithValue("$document", JsonSerializer.Serialize(cluster, JsonSettings.Options));
                clusterCommand.ExecuteNonQuery();
            }

            Assert.That(_controller!.DeleteClusterFeatureCategoryById(categoryId), Is.TypeOf<ConflictObjectResult>());
            category.Options = [];
            Assert.That(_controller.PutClusterFeatureCategoryById(categoryId, now, category), Is.TypeOf<ConflictObjectResult>());
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
