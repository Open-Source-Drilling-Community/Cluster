using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Cluster.Model;
using NORCE.Drilling.Cluster.Service.Managers;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;

namespace NORCE.Drilling.Cluster.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class SlotFeatureCategoryController : ControllerBase
    {
        private readonly ILogger<SlotFeatureCategoryManager> _logger;
        private readonly SlotFeatureCategoryManager _manager;

        public SlotFeatureCategoryController(ILogger<SlotFeatureCategoryManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
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
        public ActionResult PutSlotFeatureCategoryById(Guid id, [FromBody] Model.SlotFeatureCategory? data)
        {
            UsageStatisticsCluster.Instance.IncrementPutSlotFeatureCategoryByIdPerDay();
            if (data?.MetaInfo == null || data.MetaInfo.ID != id)
            {
                return BadRequest();
            }

            if (_manager.GetSlotFeatureCategoryById(id) == null)
            {
                return NotFound();
            }

            return _manager.UpdateSlotFeatureCategoryById(id, data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpDelete("{id:guid}", Name = "DeleteSlotFeatureCategoryById")]
        public ActionResult DeleteSlotFeatureCategoryById(Guid id)
        {
            UsageStatisticsCluster.Instance.IncrementDeleteSlotFeatureCategoryByIdPerDay();
            if (_manager.GetSlotFeatureCategoryById(id) == null)
            {
                return NotFound();
            }

            return _manager.DeleteSlotFeatureCategoryById(id)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}

