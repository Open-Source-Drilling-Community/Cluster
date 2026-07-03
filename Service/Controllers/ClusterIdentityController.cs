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
    public class ClusterIdentityController : ControllerBase
    {
        private readonly ILogger<ClusterIdentityManager> _logger;
        private readonly ClusterIdentityManager _manager;

        public ClusterIdentityController(ILogger<ClusterIdentityManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
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

        [HttpGet("HeavyData", Name = "GetAllClusterIdentity")]
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
        public ActionResult PutClusterIdentityById(Guid id, [FromBody] Model.ClusterIdentity? data)
        {
            UsageStatisticsCluster.Instance.IncrementPutClusterIdentityByIdPerDay();
            if (data?.MetaInfo == null || data.MetaInfo.ID != id)
            {
                return BadRequest();
            }

            if (_manager.GetClusterIdentityById(id) == null)
            {
                return NotFound();
            }

            return _manager.UpdateClusterIdentityById(id, data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpDelete("{id:guid}", Name = "DeleteClusterIdentityById")]
        public ActionResult DeleteClusterIdentityById(Guid id)
        {
            UsageStatisticsCluster.Instance.IncrementDeleteClusterIdentityByIdPerDay();
            if (_manager.GetClusterIdentityById(id) == null)
            {
                return NotFound();
            }

            return _manager.DeleteClusterIdentityById(id)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
