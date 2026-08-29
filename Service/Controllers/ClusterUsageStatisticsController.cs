using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Cluster.Model;

namespace OSDC.Drilling.Cluster.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class ClusterUsageStatisticsController : ControllerBase
    {
        private readonly ILogger _logger;

        public ClusterUsageStatisticsController(ILogger<ClusterUsageStatisticsController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Returns the usage statistics present in the microservice database at endpoint Cluster/api/ClusterUsageStatistics
        /// </summary>
        /// <returns></returns>
        [HttpGet(Name = "GetClusterUsageStatistics")]
        public ActionResult<UsageStatisticsCluster> GetClusterUsageStatistics()
        {
            UsageStatisticsCluster.Instance.IncrementGetClusterUsageStatisticsPerDay();
            if (UsageStatisticsCluster.Instance != null)
            {
                return Ok(UsageStatisticsCluster.Instance);
            }
            else
            {
                return NotFound();
            }
        }
    }
}
