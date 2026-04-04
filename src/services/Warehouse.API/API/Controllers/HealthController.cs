using Microsoft.AspNetCore.Mvc;

namespace Warehouse.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            service = "warehouse-api",
            status = "healthy",
            timestampUtc = DateTime.UtcNow
        });
    }
}
