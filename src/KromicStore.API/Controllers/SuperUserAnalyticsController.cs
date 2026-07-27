// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;

/// <summary>
/// Controller for SuperUser analytics dashboard.
/// </summary>
[ApiController]
[Route("api/v1/superuser/analytics")]
[Authorize(Policy = "SuperUserOnly")]
[Produces("application/json")]
public class SuperUserAnalyticsController : ControllerBase
{
    private readonly ISuperUserAnalyticsService _analyticsService;
    private readonly ILogger<SuperUserAnalyticsController> _logger;

    public SuperUserAnalyticsController(
        ISuperUserAnalyticsService analyticsService,
        ILogger<SuperUserAnalyticsController> logger)
    {
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets platform-wide analytics dashboard data.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Platform analytics data.</returns>
    /// <response code="200">Analytics data retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not a SuperUser.</response>
    /// <response code="500">Server error.</response>
    [HttpGet("platform")]
    [ProducesResponseType(typeof(PlatformAnalytics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPlatformAnalytics(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting platform analytics");

            var analytics = await _analyticsService.GetPlatformAnalyticsAsync(cancellationToken);

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting platform analytics");
            return StatusCode(500, new { error = "An error occurred while retrieving platform analytics" });
        }
    }

    /// <summary>
    /// Gets tenant health metrics.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Tenant health metrics.</returns>
    /// <response code="200">Tenant health metrics retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not a SuperUser.</response>
    /// <response code="500">Server error.</response>
    [HttpGet("tenant-health")]
    [ProducesResponseType(typeof(IEnumerable<TenantHealthMetrics>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTenantHealthMetrics(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting tenant health metrics");

            var metrics = await _analyticsService.GetTenantHealthMetricsAsync(cancellationToken);

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant health metrics");
            return StatusCode(500, new { error = "An error occurred while retrieving tenant health metrics" });
        }
    }

    /// <summary>
    /// Gets system performance metrics.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>System performance metrics.</returns>
    /// <response code="200">System performance metrics retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not a SuperUser.</response>
    /// <response code="500">Server error.</response>
    [HttpGet("system-performance")]
    [ProducesResponseType(typeof(SystemPerformanceMetrics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSystemPerformanceMetrics(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting system performance metrics");

            var metrics = await _analyticsService.GetSystemPerformanceMetricsAsync(cancellationToken);

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system performance metrics");
            return StatusCode(500, new { error = "An error occurred while retrieving system performance metrics" });
        }
    }

    /// <summary>
    /// Gets security alerts and incidents.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Security alerts.</returns>
    /// <response code="200">Security alerts retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not a SuperUser.</response>
    /// <response code="500">Server error.</response>
    [HttpGet("security-alerts")]
    [ProducesResponseType(typeof(IEnumerable<SecurityAlert>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSecurityAlerts(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting security alerts");

            var alerts = await _analyticsService.GetSecurityAlertsAsync(cancellationToken);

            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security alerts");
            return StatusCode(500, new { error = "An error occurred while retrieving security alerts" });
        }
    }
}
