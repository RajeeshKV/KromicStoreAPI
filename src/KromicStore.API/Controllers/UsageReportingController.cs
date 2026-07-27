// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;

/// <summary>
/// Controller for usage reporting and quota management.
/// </summary>
[ApiController]
[Route("api/v1/usage")]
[Authorize(Roles = "TenantAdmin")]
[Produces("application/json")]
public class UsageReportingController : BaseController
{
    private readonly IUsageReportingService _usageService;
    private readonly ILogger<UsageReportingController> _logger;

    public UsageReportingController(
        ITenantProvider tenantProvider,
        IUsageReportingService usageService,
        ILogger<UsageReportingController> logger)
        : base(tenantProvider)
    {
        _usageService = usageService ?? throw new ArgumentNullException(nameof(usageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets current usage summary for the tenant.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Usage summary with quotas and exceeded status.</returns>
    /// <response code="200">Usage summary retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="500">Server error.</response>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(UsageSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUsageSummary(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting usage summary for tenant {TenantId}", CurrentTenantId);

            var summary = await _usageService.GetUsageSummaryAsync(CurrentTenantId, cancellationToken);

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage summary for tenant {TenantId}", CurrentTenantId);
            return StatusCode(500, new { error = "An error occurred while retrieving usage summary" });
        }
    }

    /// <summary>
    /// Gets usage history for a specific type within a date range.
    /// </summary>
    /// <param name="usageType">The usage type (Storage, ApiCalls, Bandwidth, Users).</param>
    /// <param name="from">Start date.</param>
    /// <param name="to">End date.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Usage history records.</returns>
    /// <response code="200">Usage history retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="500">Server error.</response>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<Domain.Entities.TenantUsage>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUsageHistory(
        [FromQuery] string? usageType = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting usage history for tenant {TenantId}", CurrentTenantId);

            var fromDate = from ?? DateTime.UtcNow.AddMonths(-6);
            var toDate = to ?? DateTime.UtcNow;

            var history = await _usageService.GetUsageAsync(
                CurrentTenantId,
                fromDate,
                toDate,
                usageType,
                cancellationToken);

            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage history for tenant {TenantId}", CurrentTenantId);
            return StatusCode(500, new { error = "An error occurred while retrieving usage history" });
        }
    }

    /// <summary>
    /// Checks if a specific quota has been exceeded.
    /// </summary>
    /// <param name="usageType">The usage type to check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Quota exceeded status.</returns>
    /// <response code="200">Quota check completed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="500">Server error.</response>
    [HttpGet("check-quota/{usageType}")]
    [ProducesResponseType(typeof(QuotaCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckQuota(
        string usageType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking quota {UsageType} for tenant {TenantId}", usageType, CurrentTenantId);

            var isExceeded = await _usageService.CheckQuotaExceededAsync(
                CurrentTenantId,
                usageType,
                cancellationToken);

            return Ok(new QuotaCheckResponse
            {
                UsageType = usageType,
                IsExceeded = isExceeded
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking quota {UsageType} for tenant {TenantId}", usageType, CurrentTenantId);
            return StatusCode(500, new { error = "An error occurred while checking quota" });
        }
    }
}

/// <summary>
/// Response DTO for quota check.
/// </summary>
public class QuotaCheckResponse
{
    /// <summary>
    /// Gets or sets the usage type.
    /// </summary>
    public string UsageType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the quota is exceeded.
    /// </summary>
    public bool IsExceeded { get; set; }
}
