// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using KromicStore.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;
using KromicStore.Application.DTOs.Tenant;

/// <summary>
/// Controller for tenant-specific management operations.
/// Allows tenant admins to view and update their own tenant settings.
/// </summary>
[ApiController]
[Route("api/v1/tenants")]
[Authorize]
[Produces("application/json")]
public class TenantController : BaseController
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantController> _logger;

    public TenantController(
        ITenantProvider tenantProvider,
        ITenantService tenantService,
        ILogger<TenantController> logger)
        : base(tenantProvider)
    {
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets tenant details for the current user.
    /// Regular tenant admins can only access their own tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID (must match current tenant).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tenant details including subdomain, name, and configuration.</returns>
    /// <response code="200">Tenant details retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have access to this tenant.</response>
    /// <response code="404">Tenant not found.</response>
    /// <response code="500">Server error occurred.</response>
    [HttpGet("{tenantId}")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTenantDetails(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify user has access to this tenant
            if (tenantId != CurrentTenantId)
            {
                _logger.LogWarning(
                    "Unauthorized access attempt to tenant {RequestedTenantId} by user {UserId}",
                    tenantId,
                    GetCurrentUserId());
                return Forbid();
            }

            _logger.LogInformation("Retrieving tenant details for tenant {TenantId}", tenantId);

            var tenant = await _tenantService.GetTenantAsync(tenantId, cancellationToken);

            if (tenant == null)
            {
                _logger.LogWarning("Tenant {TenantId} not found", tenantId);
                return NotFound(new { error = "Tenant not found." });
            }

            return Ok(new { data = tenant });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant details for tenant {TenantId}", tenantId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving tenant details." });
        }
    }

    /// <summary>
    /// Updates tenant configuration (subdomain, name, etc).
    /// Only TenantAdmin can update their own tenant settings.
    /// </summary>
    /// <param name="tenantId">The tenant ID (must match current tenant).</param>
    /// <param name="request">Update request with new subdomain, name, etc.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated tenant details.</returns>
    /// <response code="200">Tenant updated successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have access to this tenant.</response>
    /// <response code="404">Tenant not found.</response>
    /// <response code="409">Subdomain already taken or conflict occurred.</response>
    /// <response code="500">Server error occurred.</response>
    [HttpPut("{tenantId}")]
    [Authorize(Policy = Permissions.SettingsWrite)]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateTenant(
        Guid tenantId,
        [FromBody] UpdateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify user has access to this tenant
            if (tenantId != CurrentTenantId)
            {
                _logger.LogWarning(
                    "Unauthorized update attempt to tenant {RequestedTenantId} by user {UserId}",
                    tenantId,
                    GetCurrentUserId());
                return Forbid();
            }

            if (request == null)
            {
                return BadRequest(new { error = "Request body cannot be null." });
            }

            _logger.LogInformation("Updating tenant {TenantId}", tenantId);

            var result = await _tenantService.UpdateTenantAsync(tenantId, request, cancellationToken);

            _logger.LogInformation("Tenant {TenantId} updated successfully", tenantId);

            return Ok(new
            {
                data = result,
                message = "Tenant updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant {TenantId}", tenantId);
            
            if (ex.Message.Contains("Subdomain"))
            {
                return Conflict(new { error = ex.Message });
            }

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while updating tenant." });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value
            ?? User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        return !string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
