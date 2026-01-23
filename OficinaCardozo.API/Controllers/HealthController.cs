using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OficinaCardozo.Infrastructure.Data;

namespace OficinaCardozo.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly OficinaDbContext _context;

    public HealthController(OficinaDbContext context)
    {
        _context = context;
        Console.WriteLine($"[HealthController] Instanciado em {DateTime.UtcNow:O}");
    }

    [HttpGet("live")]
    public IActionResult Live()
    {
        Console.WriteLine($"[HealthController] Live endpoint chamado em {DateTime.UtcNow:O}");
        return Ok(new { status = "Live" });
    }


    [HttpGet("ping")]
    public IActionResult Ping()
    {
        Console.WriteLine($"[HealthController] Ping endpoint chamado em {DateTime.UtcNow:O}");
        try
        {
            StatsdClient.Metrics.Counter("healthcheck.success", 1);
            Console.WriteLine($"[HealthController] Ping sucesso em {DateTime.UtcNow:O}");
            return Ok(new
            {
                status = "Alive",
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                lambda = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"))
            });
        }
        catch (Exception ex)
        {
            StatsdClient.Metrics.Counter("healthcheck.degraded", 1);
            Console.WriteLine($"[HealthController] ERRO no Ping: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
            // Nunca retorna 500 para o probe, sempre 200 com status degradado
            return Ok(new {
                status = "Degraded",
                error = ex.Message,
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                lambda = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"))
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            // Timeout de 5 segundos para não travar
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var canConnect = await _context.Database.CanConnectAsync(cts.Token);

            StatsdClient.Metrics.Counter("healthcheck.success", 1);

            return Ok(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                database = canConnect ? "Connected" : "Disconnected"
            });
        }
        catch (OperationCanceledException)
        {
            StatsdClient.Metrics.Counter("healthcheck.fail", 1);
            return StatusCode(503, new
            {
                status = "Unhealthy",
                timestamp = DateTime.UtcNow,
                error = "Database connection timeout (5s)",
                database = "Timeout"
            });
        }
        catch (Exception ex)
        {
            StatsdClient.Metrics.Counter("healthcheck.fail", 1);
            return StatusCode(503, new
            {
                status = "Unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message,
                errorType = ex.GetType().Name,
                database = "Disconnected"
            });
        }
    }

}