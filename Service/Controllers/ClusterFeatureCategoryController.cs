using OSDC.Drilling.Cluster.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Cluster.Model;
using OSDC.Drilling.Cluster.Service.Managers;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Cluster.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class ClusterFeatureCategoryController : ControllerBase
    {
        private readonly SqlConnectionManager _discoveryConnections;

        /// <summary>Discover ClusterFeatureCategory resources as bounded light records; retrieve selected full records by ID.</summary>
        [HttpGet("LightData", Name = "DiscoverClusterFeatureCategory")]
        public ActionResult<OSDC.Drilling.Cluster.Model.ClusterFeatureCategoryDiscoveryPage> DiscoverClusterFeatureCategory([FromQuery] int offset = 0, [FromQuery] int limit = 50, [FromQuery] string? name = null)
        {
            UsageStatisticsCluster.Instance.IncrementDiscoveryRequestsPerDay();
            if (offset < 0 || limit < 1 || limit > 200 || name?.Length > 200)
                return BadRequest(new { error = "invalid_discovery_query", message = "Offset must be non-negative, limit 1–200, name at most 200 characters, and UUID filters non-empty." });
            try
            {
                var records = DiscoveryRecords.ReadPage<OSDC.Drilling.Cluster.Model.ClusterFeatureCategoryLight>(_discoveryConnections.GetConnection(), "ClusterFeatureCategoryTable", "ClusterFeatureCategory", offset, limit, name,
                    new System.Collections.Generic.Dictionary<string, object?> {  }, out int total);
                return Ok(new OSDC.Drilling.Cluster.Model.ClusterFeatureCategoryDiscoveryPage { Items = records, Offset = offset, Limit = limit, TotalCount = total,
                    NextOffset = offset < total && records.Count < total - offset ? offset + records.Count : null });
            }
            catch (System.Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException or System.Text.Json.JsonException or System.InvalidOperationException)
            {
                return StatusCode(500, new { error = "discovery_failed" });
            }
        }

        private readonly ILogger<ClusterFeatureCategoryManager> _logger;
        private readonly ClusterFeatureCategoryManager _manager;
        private readonly SqlConnectionManager _connectionManager;

        public ClusterFeatureCategoryController(ILogger<ClusterFeatureCategoryManager> logger, SqlConnectionManager connectionManager)
        {
            _discoveryConnections = connectionManager;
            _logger = logger;
            _connectionManager = connectionManager;
            _manager = ClusterFeatureCategoryManager.GetInstance(_logger, connectionManager);
        }

        [HttpGet(Name = "GetAllClusterFeatureCategoryId")]
        public ActionResult<IEnumerable<Guid>> GetAllClusterFeatureCategoryId()
        {
            UsageStatisticsCluster.Instance.IncrementGetAllClusterFeatureCategoryIdPerDay();
            var ids = _manager.GetAllClusterFeatureCategoryId();
            return ids != null ? Ok(ids) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllClusterFeatureCategoryMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo?>> GetAllClusterFeatureCategoryMetaInfo()
        {
            UsageStatisticsCluster.Instance.IncrementGetAllClusterFeatureCategoryMetaInfoPerDay();
            var metaInfos = _manager.GetAllClusterFeatureCategoryMetaInfo();
            return metaInfos != null ? Ok(metaInfos) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id:guid}", Name = "GetClusterFeatureCategoryById")]
        public ActionResult<Model.ClusterFeatureCategory?> GetClusterFeatureCategoryById(Guid id)
        {
            UsageStatisticsCluster.Instance.IncrementGetClusterFeatureCategoryByIdPerDay();
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            var data = _manager.GetClusterFeatureCategoryById(id);
            return data != null ? Ok(data) : NotFound();
        }

        [NonAction]
        public ActionResult<IEnumerable<Model.ClusterFeatureCategory?>> GetAllClusterFeatureCategory()
        {
            UsageStatisticsCluster.Instance.IncrementGetAllClusterFeatureCategoryPerDay();
            var data = _manager.GetAllClusterFeatureCategory();
            return data != null ? Ok(data) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPost(Name = "PostClusterFeatureCategory")]
        public ActionResult PostClusterFeatureCategory([FromBody] Model.ClusterFeatureCategory? data)
        {
            UsageStatisticsCluster.Instance.IncrementPostClusterFeatureCategoryPerDay();
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                return BadRequest();
            }

            if (_manager.GetClusterFeatureCategoryById(data.MetaInfo.ID) != null)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }

            return _manager.AddClusterFeatureCategory(data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut("{id:guid}", Name = "PutClusterFeatureCategoryById")]
        [ProducesResponseType(typeof(Model.ClusterFeatureCategory), StatusCodes.Status200OK)]
        public ActionResult PutClusterFeatureCategoryById(Guid id, [FromQuery, BindRequired] DateTimeOffset expectedModifiedUtc, [FromBody] Model.ClusterFeatureCategory? data)
        {
            UsageStatisticsCluster.Instance.IncrementPutClusterFeatureCategoryByIdPerDay();
            if (expectedModifiedUtc == default) return BadRequest(new ClusterMutationErrorEnvelope { Error = "invalid_request", Message = "expectedModifiedUtc is required." });
            if (data == null) return BadRequest(new ClusterMutationErrorEnvelope { Error = "invalid_request", Message = "clusterFeatureCategory is required." });
            return this.ToActionResult(ClusterCatalogMutationManager.UpdateClusterCategory(_connectionManager, _logger, id, expectedModifiedUtc, data), data);
        }

        [HttpDelete("{id:guid}", Name = "DeleteClusterFeatureCategoryById")]
        public ActionResult DeleteClusterFeatureCategoryById(Guid id)
        {
            UsageStatisticsCluster.Instance.IncrementDeleteClusterFeatureCategoryByIdPerDay();
            return this.ToActionResult(ClusterCatalogMutationManager.DeleteClusterCategory(_connectionManager, _logger, id));
        }
    }
}
