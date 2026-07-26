namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Health check endpoint controller.
/// Provides liveness and readiness checks with Redis keep-alive.
/// </summary>
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    /// <summary>
    /// Liveness check - basic health status (always returns 200 if app is running).
    /// </summary>
    [HttpGet]
    [HttpHead]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHealth()
    {
        var report = await _healthCheckService.CheckHealthAsync();
        
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }

    /// <summary>
    /// Readiness check - verifies all dependencies (database, Redis, etc.) are accessible.
    /// This also keeps Redis active by pinging it on each check.
    /// </summary>
    [HttpGet("ready")]
    [HttpHead("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetReadiness()
    {
        var report = await _healthCheckService.CheckHealthAsync();

        var checks = new Dictionary<string, object>();
        foreach (var entry in report.Entries)
        {
            checks[entry.Key] = new
            {
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds,
                data = entry.Value.Data?.Count > 0 ? entry.Value.Data : null
            };
        }

        var readiness = new
        {
            status = report.Status.ToString(),
            checks = checks,
            totalDuration = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTime.UtcNow
        };

        if (report.Status == HealthStatus.Unhealthy)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, readiness);
        }

        return Ok(readiness);
    }
}
