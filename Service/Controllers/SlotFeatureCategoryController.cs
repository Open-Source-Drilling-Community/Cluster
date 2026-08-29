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
        private readonly ILogger<SlotFeatureCategoryManager> _logger;
        private readonly SlotFeatureCategoryManager _manager;
        private readonly SqlConnectionManager _connectionManager;

        public SlotFeatureCategoryController(ILogger<SlotFeatureCategoryManager> logger, SqlConnectionManager connectionManager)
        {
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

        [HttpGet("HeavyData", Name = "GetAllSlotFeatureCategory")]
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

