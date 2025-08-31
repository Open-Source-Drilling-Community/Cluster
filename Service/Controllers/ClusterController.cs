using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using NORCE.Drilling.Cluster.Service.Managers;
using NORCE.Drilling.Cluster.Model;

namespace NORCE.Drilling.Cluster.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class ClusterController : ControllerBase
    {
        private readonly ILogger<ClusterManager> _logger;
        private readonly ClusterManager _clusterManager;

        public ClusterController(ILogger<ClusterManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _clusterManager = ClusterManager.GetInstance(_logger, connectionManager);
        }

        /// <summary>
        /// Returns the list of Guid of all Cluster present in the microservice database at endpoint Cluster/api/Cluster
        /// </summary>
        /// <returns>the list of Guid of all Cluster present in the microservice database at endpoint Cluster/api/Cluster</returns>
        [HttpGet(Name = "GetAllClusterId")]
        public ActionResult<IEnumerable<Guid>> GetAllClusterId()
        {
            UsageStatisticsCluster.Instance.IncrementGetAllClusterIdPerDay();
            var ids = _clusterManager.GetAllClusterId();
            if (ids != null)
            {
                return Ok(ids);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Returns the list of MetaInfo of all Cluster present in the microservice database, at endpoint Cluster/api/Cluster/MetaInfo
        /// </summary>
        /// <returns>the list of MetaInfo of all Cluster present in the microservice database, at endpoint Cluster/api/Cluster/MetaInfo</returns>
        [HttpGet("MetaInfo", Name = "GetAllClusterMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo>> GetAllClusterMetaInfo()
        {
            UsageStatisticsCluster.Instance.IncrementGetAllClusterMetaInfoPerDay();
            var vals = _clusterManager.GetAllClusterMetaInfo();
            if (vals != null)
            {
                return Ok(vals);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Returns the Cluster identified by its Guid from the microservice database, at endpoint Cluster/api/Cluster/MetaInfo/id
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>the Cluster identified by its Guid from the microservice database, at endpoint Cluster/api/Cluster/MetaInfo/id</returns>
        [HttpGet("{id}", Name = "GetClusterById")]
        public ActionResult<Model.Cluster?> GetClusterById(Guid id)
        {
            UsageStatisticsCluster.Instance.IncrementGetClusterByIdPerDay();
            if (!id.Equals(Guid.Empty))
            {
                var val = _clusterManager.GetClusterById(id);
                if (val != null)
                {
                    return Ok(val);
                }
                else
                {
                    return NotFound();
                }
            }
            else
            {
                return BadRequest();
            }
        }


        /// <summary>
        /// Returns the list of all Cluster present in the microservice database, at endpoint Cluster/api/Cluster/HeavyData
        /// </summary>
        /// <returns>the list of all Cluster present in the microservice database, at endpoint Cluster/api/Cluster/HeavyData</returns>
        [HttpGet("HeavyData", Name = "GetAllCluster")]
        public ActionResult<IEnumerable<Model.Cluster?>> GetAllCluster()
        {
            UsageStatisticsCluster.Instance.IncrementGetAllClusterPerDay();
            var vals = _clusterManager.GetAllCluster();
            if (vals != null)
            {
                return Ok(vals);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Performs calculation on the given Cluster and adds it to the microservice database, at the endpoint Cluster/api/Cluster
        /// </summary>
        /// <param name="cluster"></param>
        /// <returns>true if the given Cluster has been added successfully to the microservice database, at the endpoint Cluster/api/Cluster</returns>
        [HttpPost(Name = "PostCluster")]
        public ActionResult PostCluster([FromBody] Model.Cluster? data)
        {
            UsageStatisticsCluster.Instance.IncrementPostClusterPerDay();
            // Check if cluster exists in the database through ID
            if (data != null && data.MetaInfo != null && data.MetaInfo.ID != Guid.Empty)
            {
                var existingData = _clusterManager.GetClusterById(data.MetaInfo.ID);
                if (existingData == null)
                {   
                    //  If cluster was not found, call AddCluster, where the cluster.Calculate()
                    // method is called. 
                    if (_clusterManager.AddCluster(data))
                    {
                        return Ok(); // status=OK is used rather than status=Created because NSwag auto-generated controllers use 200 (OK) rather than 201 (Created) as return codes
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
                else
                {
                    _logger.LogWarning("The given Cluster already exists and will not be added");
                    return StatusCode(StatusCodes.Status409Conflict);
                }
            }
            else
            {
                _logger.LogWarning("The given Cluster is null, badly formed, or its ID is empty");
                return BadRequest();
            }
        }

        /// <summary>
        /// Performs calculation on the given Cluster and updates it in the microservice database, at the endpoint Cluster/api/Cluster/id
        /// </summary>
        /// <param name="cluster"></param>
        /// <returns>true if the given Cluster has been updated successfully to the microservice database, at the endpoint Cluster/api/Cluster/id</returns>
        [HttpPut("{id}", Name = "PutClusterById")]
        public ActionResult PutClusterById(Guid id, [FromBody] Model.Cluster? data)
        {
            UsageStatisticsCluster.Instance.IncrementPutClusterByIdPerDay();
            // Check if Cluster is in the data base
            if (data != null && data.MetaInfo != null && data.MetaInfo.ID.Equals(id))
            {
                var existingData = _clusterManager.GetClusterById(id);
                if (existingData != null)
                {
                    if (_clusterManager.UpdateClusterById(id, data))
                    {
                        return Ok();
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
                else
                {
                    _logger.LogWarning("The given Cluster has not been found in the database");
                    return NotFound();
                }
            }
            else
            {
                _logger.LogWarning("The given Cluster is null, badly formed, or its does not match the ID to update");
                return BadRequest();
            }
        }

        /// <summary>
        /// Deletes the Cluster of given ID from the microservice database, at the endpoint Cluster/api/Cluster/id
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>true if the Cluster was deleted from the microservice database, at the endpoint Cluster/api/Cluster/id</returns>
        [HttpDelete("{id}", Name = "DeleteClusterById")]
        public ActionResult DeleteClusterById(Guid id)
        {
            UsageStatisticsCluster.Instance.IncrementDeleteClusterByIdPerDay();
            if (_clusterManager.GetClusterById(id) != null)
            {
                if (_clusterManager.DeleteClusterById(id))
                {
                    return Ok();
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
            }
            else
            {
                _logger.LogWarning("The Cluster of given ID does not exist");
                return NotFound();
            }
        }
    }
}
