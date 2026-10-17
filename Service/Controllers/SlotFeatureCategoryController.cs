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
    public class SlotFeatureCategoryController : ControllerBase
    {
        private readonly SqlConnectionManager _discoveryConnections;

        /// <summary>Discover SlotFeatureCategory resources as bounded light records; retrieve selected full records by ID.</summary>
        [HttpGet("LightData", Name = "DiscoverSlotFeatureCategory")]
        public ActionResult<OSDC.Drilling.Cluster.Model.SlotFeatureCategoryDiscoveryPage> DiscoverSlotFeatureCategory([FromQuery] int offset = 0, [FromQuery] int limit = 50, [FromQuery] string? name = null)
        {
            UsageStatisticsCluster.Instance.IncrementDiscoveryRequestsPerDay();
            if (offset < 0 || limit < 1 || limit > 200 || name?.Length > 200)
                return BadRequest(new { error = "invalid_discovery_query", message = "Offset must be non-negative, limit 1–200, name at most 200 characters, and UUID filters non-empty." });
            try
            {
                var records = DiscoveryRecords.ReadPage<OSDC.Drilling.Cluster.Model.SlotFeatureCategoryLight>(_discoveryConnections.GetConnection(), "SlotFeatureCategoryTable", "SlotFeatureCategory", offset, limit, name,
                    new System.Collections.Generic.Dictionary<string, object?> {  }, out int total);
                return Ok(new OSDC.Drilling.Cluster.Model.SlotFeatureCategoryDiscoveryPage { Items = records, Offset = offset, Limit = limit, TotalCount = total,
                    NextOffset = offset < total && records.Count < total - offset ? offset + records.Count : null });
            }
            catch (System.Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException or System.Text.Json.JsonException or System.InvalidOperationException)
            {
                return StatusCode(500, new { error = "discovery_failed" });
            }
        }

        private readonly ILogger<SlotFeatureCategoryManager> _logger;
        private readonly SlotFeatureCategoryManager _manager;
        private readonly SqlConnectionManager _connectionManager;

        public SlotFeatureCategoryController(ILogger<SlotFeatureCategoryManager> logger, SqlConnectionManager connectionManager)
        {
            _discoveryConnections = connectionManager;
            _logger = logger;
            _connectionManager = connectionManager;
            _manager = SlotFeatureCategoryManager.GetInstance(_logger, connectionManager);
        }

        [HttpGet(Name = "GetAllSlotFeatureCategoryId")]
        public ActionResult<IEnumerable<Guid>> GetAllSlotFeatureCategoryId()
        {
            UsageStatisticsCluster.Instance.IncrementGetAllSlotFeatureCategoryIdPerDay();
            var ids = _manager.GetAllSlotFeatureCategoryId();
            return ids != null ? Ok(ids) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllSlotFeatureCategoryMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo?>> GetAllSlotFeatureCategoryMetaInfo()
        {
            UsageStatisticsCluster.Instance.IncrementGetAllSlotFeatureCategoryMetaInfoPerDay();
            var metaInfos = _manager.GetAllSlotFeatureCategoryMetaInfo();
            return metaInfos != null ? Ok(metaInfos) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id:guid}", Name = "GetSlotFeatureCategoryById")]
        public ActionResult<Model.SlotFeatureCategory?> GetSlotFeatureCategoryById(Guid id)
        {
            UsageStatisticsCluster.Instance.IncrementGetSlotFeatureCategoryByIdPerDay();
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            var data = _manager.GetSlotFeatureCategoryById(id);
            return data != null ? Ok(data) : NotFound();
        }

        [NonAction]
        public ActionResult<IEnumerable<Model.SlotFeatureCategory?>> GetAllSlotFeatureCategory()
        {
            UsageStatisticsCluster.Instance.IncrementGetAllSlotFeatureCategoryPerDay();
            var data = _manager.GetAllSlotFeatureCategory();
            return data != null ? Ok(data) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPost(Name = "PostSlotFeatureCategory")]
        public ActionResult PostSlotFeatureCategory([FromBody] Model.SlotFeatureCategory? data)
        {
            UsageStatisticsCluster.Instance.IncrementPostSlotFeatureCategoryPerDay();
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                return BadRequest();
            }

            if (_manager.GetSlotFeatureCategoryById(data.MetaInfo.ID) != null)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }

            return _manager.AddSlotFeatureCategory(data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut("{id:guid}", Name = "PutSlotFeatureCategoryById")]
        [ProducesResponseType(typeof(Model.SlotFeatureCategory), StatusCodes.Status200OK)]
        public ActionResult PutSlotFeatureCategoryById(Guid id, [FromQuery, BindRequired] DateTimeOffset expectedModifiedUtc, [FromBody] Model.SlotFeatureCategory? data)
        {
            UsageStatisticsCluster.Instance.IncrementPutSlotFeatureCategoryByIdPerDay();
            if (expectedModifiedUtc == default) return BadRequest(new ClusterMutationErrorEnvelope { Error = "invalid_request", Message = "expectedModifiedUtc is required." });
            if (data == null) return BadRequest(new ClusterMutationErrorEnvelope { Error = "invalid_request", Message = "slotFeatureCategory is required." });
            return this.ToActionResult(ClusterCatalogMutationManager.UpdateSlotCategory(_connectionManager, _logger, id, expectedModifiedUtc, data), data);
        }

        [HttpDelete("{id:guid}", Name = "DeleteSlotFeatureCategoryById")]
        public ActionResult DeleteSlotFeatureCategoryById(Guid id)
        {
            UsageStatisticsCluster.Instance.IncrementDeleteSlotFeatureCategoryByIdPerDay();
            return this.ToActionResult(ClusterCatalogMutationManager.DeleteSlotCategory(_connectionManager, _logger, id));
        }
    }
}

