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
    public class ClusterIdentityController : ControllerBase
    {
        private readonly SqlConnectionManager _discoveryConnections;

        /// <summary>Discover ClusterIdentity resources as bounded light records; retrieve selected full records by ID.</summary>
        [HttpGet("LightData", Name = "DiscoverClusterIdentity")]
        public ActionResult<OSDC.Drilling.Cluster.Model.ClusterIdentityDiscoveryPage> DiscoverClusterIdentity([FromQuery] int offset = 0, [FromQuery] int limit = 50, [FromQuery] string? name = null)
        {
            UsageStatisticsCluster.Instance.IncrementDiscoveryRequestsPerDay();
            if (offset < 0 || limit < 1 || limit > 200 || name?.Length > 200)
                return BadRequest(new { error = "invalid_discovery_query", message = "Offset must be non-negative, limit 1–200, name at most 200 characters, and UUID filters non-empty." });
            try
            {
                var records = DiscoveryRecords.ReadPage<OSDC.Drilling.Cluster.Model.ClusterIdentityLight>(_discoveryConnections.GetConnection(), "ClusterIdentityTable", "ClusterIdentity", offset, limit, name,
                    new System.Collections.Generic.Dictionary<string, object?> {  }, out int total);
                return Ok(new OSDC.Drilling.Cluster.Model.ClusterIdentityDiscoveryPage { Items = records, Offset = offset, Limit = limit, TotalCount = total,
                    NextOffset = offset < total && records.Count < total - offset ? offset + records.Count : null });
            }
            catch (System.Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException or System.Text.Json.JsonException or System.InvalidOperationException)
            {
                return StatusCode(500, new { error = "discovery_failed" });
            }
        }

        private readonly ILogger<ClusterIdentityManager> _logger;
        private readonly ClusterIdentityManager _manager;
        private readonly SqlConnectionManager _connectionManager;

        public ClusterIdentityController(ILogger<ClusterIdentityManager> logger, SqlConnectionManager connectionManager)
        {
            _discoveryConnections = connectionManager;
            _logger = logger;
            _connectionManager = connectionManager;
            _manager = ClusterIdentityManager.GetInstance(_logger, connectionManager);
        }

        [HttpGet(Name = "GetAllClusterIdentityId")]
        public ActionResult<IEnumerable<Guid>> GetAllClusterIdentityId()
        {
            UsageStatisticsCluster.Instance.IncrementGetAllClusterIdentityIdPerDay();
            var ids = _manager.GetAllClusterIdentityId();
            return ids != null ? Ok(ids) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllClusterIdentityMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo?>> GetAllClusterIdentityMetaInfo()
        {
            UsageStatisticsCluster.Instance.IncrementGetAllClusterIdentityMetaInfoPerDay();
            var metaInfos = _manager.GetAllClusterIdentityMetaInfo();
            return metaInfos != null ? Ok(metaInfos) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id:guid}", Name = "GetClusterIdentityById")]
        public ActionResult<Model.ClusterIdentity?> GetClusterIdentityById(Guid id)
        {
            UsageStatisticsCluster.Instance.IncrementGetClusterIdentityByIdPerDay();
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            var data = _manager.GetClusterIdentityById(id);
            return data != null ? Ok(data) : NotFound();
        }

        [NonAction]
        public ActionResult<IEnumerable<Model.ClusterIdentity?>> GetAllClusterIdentity()
        {
            UsageStatisticsCluster.Instance.IncrementGetAllClusterIdentityPerDay();
            var data = _manager.GetAllClusterIdentity();
            return data != null ? Ok(data) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPost(Name = "PostClusterIdentity")]
        public ActionResult PostClusterIdentity([FromBody] Model.ClusterIdentity? data)
        {
            UsageStatisticsCluster.Instance.IncrementPostClusterIdentityPerDay();
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                return BadRequest();
            }

            if (_manager.GetClusterIdentityById(data.MetaInfo.ID) != null)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }

            return _manager.AddClusterIdentity(data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut("{id:guid}", Name = "PutClusterIdentityById")]
        [ProducesResponseType(typeof(Model.ClusterIdentity), StatusCodes.Status200OK)]
        public ActionResult PutClusterIdentityById(Guid id, [FromQuery, BindRequired] DateTimeOffset expectedModifiedUtc, [FromBody] Model.ClusterIdentity? data)
        {
            UsageStatisticsCluster.Instance.IncrementPutClusterIdentityByIdPerDay();
            if (expectedModifiedUtc == default) return BadRequest(new ClusterMutationErrorEnvelope { Error = "invalid_request", Message = "expectedModifiedUtc is required." });
            if (data == null) return BadRequest(new ClusterMutationErrorEnvelope { Error = "invalid_request", Message = "clusterIdentity is required." });
            return this.ToActionResult(ClusterCatalogMutationManager.UpdateIdentity(_connectionManager, _logger, id, expectedModifiedUtc, data), data);
        }

        [HttpDelete("{id:guid}", Name = "DeleteClusterIdentityById")]
        public ActionResult DeleteClusterIdentityById(Guid id)
        {
            UsageStatisticsCluster.Instance.IncrementDeleteClusterIdentityByIdPerDay();
            return this.ToActionResult(ClusterCatalogMutationManager.DeleteIdentity(_connectionManager, _logger, id));
        }
    }
}
