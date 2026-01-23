using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace OficinaCardozo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PingController : ControllerBase
{
    private readonly ILogger<PingController> _logger;
    public PingController(ILogger<PingController> logger)
    {
        _logger = logger;
    }
    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogInformation("Ping endpoint chamado");
        return Ok(new
        {
            status = "OK",
            message = "Lambda is working!",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}
