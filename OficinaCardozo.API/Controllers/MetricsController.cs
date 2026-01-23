using Microsoft.AspNetCore.Mvc;
using System;

namespace OficinaCardozo.API.Controllers
{
    [ApiController]
    [Route("api/metrics")]
    public class MetricsController : ControllerBase
    {
        [HttpPost("test-batch")]
        public IActionResult SendBatchMetrics([FromQuery] int count = 100)
        {
            for (int i = 0; i < count; i++)
            {
                StatsdClient.Metrics.Counter("echo_teste.metric", 1);
            }
            return Ok(new { sent = count, timestamp = DateTime.UtcNow });
        }
    }
}
