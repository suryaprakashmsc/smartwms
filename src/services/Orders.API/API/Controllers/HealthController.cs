using Microsoft.AspNetCore.Mvc;

namespace Orders.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            service = "orders-api",
            status = "healthy",
            timestampUtc = DateTime.UtcNow
        });
    }
}
