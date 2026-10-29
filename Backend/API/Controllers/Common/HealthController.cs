using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Common;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            timestampUtc = DateTime.UtcNow,
            service = "Social API"
        });
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok("pong");
    }
}

