using Microsoft.AspNetCore.Mvc;
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
        Console.WriteLine($"[HealthController] Instanciado com DbContext em {DateTime.UtcNow:O}");
    }

    [HttpGet("live")]
    public IActionResult Live()
    {
        Console.WriteLine($"[HealthController] Live endpoint chamado em {DateTime.UtcNow:O}");
        return Ok(new { status = "Live" });
    }
}