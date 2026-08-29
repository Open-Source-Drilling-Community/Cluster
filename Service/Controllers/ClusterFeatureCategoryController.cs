using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        private readonly ILogger<ClusterFeatureCategoryManager> _logger;
        private readonly ClusterFeatureCategoryManager _manager;

        public ClusterFeatureCategoryController(ILogger<ClusterFeatureCategoryManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
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

        [HttpGet("HeavyData", Name = "GetAllClusterFeatureCategory")]
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
        public ActionResult PutClusterFeatureCategoryById(Guid id, [FromBody] Model.ClusterFeatureCategory? data)
        {
            UsageStatisticsCluster.Instance.IncrementPutClusterFeatureCategoryByIdPerDay();
            if (data?.MetaInfo == null || data.MetaInfo.ID != id)
            {
                return BadRequest();
            }

            if (_manager.GetClusterFeatureCategoryById(id) == null)
            {
                return NotFound();
            }

            return _manager.UpdateClusterFeatureCategoryById(id, data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpDelete("{id:guid}", Name = "DeleteClusterFeatureCategoryById")]
        public ActionResult DeleteClusterFeatureCategoryById(Guid id)
        {
            UsageStatisticsCluster.Instance.IncrementDeleteClusterFeatureCategoryByIdPerDay();
            if (_manager.GetClusterFeatureCategoryById(id) == null)
            {
                return NotFound();
            }

            return _manager.DeleteClusterFeatureCategoryById(id)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
