using Microsoft.AspNetCore.Mvc;

namespace OFICINACARDOZO.OSSERVICE.API
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { Message = "OS Service API rodando!" });
        }
    }
}
