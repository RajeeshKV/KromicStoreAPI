namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;

/// <summary>
/// Health check endpoint controller.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Gets the health status of the API.
    /// </summary>
    /// <returns>Health status response.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}
