using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OficinaCardozo.Application.Interfaces;

namespace OficinaCardozo.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly IHealthService _healthService;

    public HealthController(IHealthService healthService, ILogger<HealthController> logger)
    {
        _healthService = healthService;
        _logger = logger;
        _logger.LogInformation("[HealthController] Instanciado com IHealthService em {Time}", DateTime.UtcNow);
    }

    [HttpGet("live")]
    public IActionResult Live()
    {
        _logger.LogInformation("[HealthController] Live endpoint chamado em {Time}", DateTime.UtcNow);
        var dbHealthy = _healthService.IsDatabaseHealthy();
        return Ok(new { status = "Live", dbHealthy });
    }
}