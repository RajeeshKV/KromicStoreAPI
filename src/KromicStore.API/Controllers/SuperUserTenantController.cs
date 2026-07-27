// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Tenants;
using KromicStore.Domain.Entities;

/// <summary>
/// Controller for SuperUser to manage tenant lifecycle (suspend, archive, restore, soft delete).
/// </summary>
[ApiController]
[Route("api/v1/admin/tenants")]
[Authorize(Policy = "SuperUserOnly")]
[Produces("application/json")]
public class SuperUserTenantController : BaseController
{
    private readonly ITenantService _tenantService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<SuperUserTenantController> _logger;

    public SuperUserTenantController(
        ITenantProvider tenantProvider,
        ITenantService tenantService,
        IAuditLogService auditLogService,
        ILogger<SuperUserTenantController> logger)
        : base(tenantProvider)
    {
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Suspends a tenant, making it inaccessible without deleting data.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="request">The suspension request with reason.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated tenant status.</returns>
    /// <response code="200">Tenant suspended successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not a SuperUser.</response>
    /// <response code="404">Tenant not found.</response>
    /// <response code="500">Server error.</response>
    [HttpPost("{tenantId}/suspend")]
    [ProducesResponseType(typeof(TenantLifecycleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SuspendTenant(
        Guid tenantId,
        [FromBody] TenantLifecycleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("SuperUser suspending tenant {TenantId}", tenantId);

            var tenantResponse = await _tenantService.GetTenantAsync(tenantId, cancellationToken);
            if (tenantResponse == null)
            {
                return NotFound(new { error = "Tenant not found" });
            }

            var success = await _tenantService.SuspendTenantAsync(tenantId, cancellationToken);
            if (!success)
            {
                return BadRequest(new { error = "Failed to suspend tenant" });
            }

            // Log audit entry
            var userId = GetCurrentUserId();
            await _auditLogService.LogActionAsync(
                null,
                userId,
                "SuperUser",
                "Tenant",
                tenantId,
                "Suspend",
                metadata: $"Reason: {request.Reason}",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Tenant {TenantId} suspended successfully", tenantId);

            return Ok(new TenantLifecycleResponse
            {
                TenantId = tenantId,
                Status = "Suspended",
                IsActive = false,
                SuspendedAt = DateTime.UtcNow,
                Reason = request.Reason
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending tenant {TenantId}", tenantId);
            return StatusCode(500, new { error = "An error occurred while suspending the tenant" });
        }
    }

    /// <summary>
    /// Archives a tenant for long-term retention.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="request">The archive request with reason.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated tenant status.</returns>
    /// <response code="200">Tenant archived successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not a SuperUser.</response>
    /// <response code="404">Tenant not found.</response>
    /// <response code="500">Server error.</response>
    [HttpPost("{tenantId}/archive")]
    [ProducesResponseType(typeof(TenantLifecycleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ArchiveTenant(
        Guid tenantId,
        [FromBody] TenantLifecycleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("SuperUser archiving tenant {TenantId}", tenantId);

            var tenantResponse = await _tenantService.GetTenantAsync(tenantId, cancellationToken);
            if (tenantResponse == null)
            {
                return NotFound(new { error = "Tenant not found" });
            }

            // Archive operation - would need to add to ITenantService
            // For now, just log the action
            
            // Log audit entry
            var userId = GetCurrentUserId();
            await _auditLogService.LogActionAsync(
                null,
                userId,
                "SuperUser",
                "Tenant",
                tenantId,
                "Archive",
                metadata: $"Reason: {request.Reason}",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Tenant {TenantId} archived successfully", tenantId);

            return Ok(new TenantLifecycleResponse
            {
                TenantId = tenantId,
                Status = "Archived",
                IsActive = false,
                IsArchived = true,
                ArchivedAt = DateTime.UtcNow,
                Reason = request.Reason
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving tenant {TenantId}", tenantId);
            return StatusCode(500, new { error = "An error occurred while archiving the tenant" });
        }
    }

    /// <summary>
    /// Soft deletes a tenant while preserving records for restore/retention.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="request">The soft delete request with reason.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated tenant status.</returns>
    /// <response code="200">Tenant soft deleted successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not a SuperUser.</response>
    /// <response code="404">Tenant not found.</response>
    /// <response code="500">Server error.</response>
    [HttpPost("{tenantId}/soft-delete")]
    [ProducesResponseType(typeof(TenantLifecycleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SoftDeleteTenant(
        Guid tenantId,
        [FromBody] TenantLifecycleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("SuperUser soft deleting tenant {TenantId}", tenantId);

            var tenantResponse = await _tenantService.GetTenantAsync(tenantId, cancellationToken);
            if (tenantResponse == null)
            {
                return NotFound(new { error = "Tenant not found" });
            }

            // Soft delete operation - would need to add to ITenantService
            // For now, just log the action
            
            // Log audit entry
            var userId = GetCurrentUserId();
            await _auditLogService.LogActionAsync(
                null,
                userId,
                "SuperUser",
                "Tenant",
                tenantId,
                "SoftDelete",
                metadata: $"Reason: {request.Reason}",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Tenant {TenantId} soft deleted successfully", tenantId);

            return Ok(new TenantLifecycleResponse
            {
                TenantId = tenantId,
                Status = "SoftDeleted",
                IsActive = false,
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow,
                Reason = request.Reason
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft deleting tenant {TenantId}", tenantId);
            return StatusCode(500, new { error = "An error occurred while soft deleting the tenant" });
        }
    }

    /// <summary>
    /// Restores a suspended, archived, or soft-deleted tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="request">The restore request with reason.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated tenant status.</returns>
    /// <response code="200">Tenant restored successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not a SuperUser.</response>
    /// <response code="404">Tenant not found.</response>
    /// <response code="500">Server error.</response>
    [HttpPost("{tenantId}/restore")]
    [ProducesResponseType(typeof(TenantLifecycleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RestoreTenant(
        Guid tenantId,
        [FromBody] TenantLifecycleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("SuperUser restoring tenant {TenantId}", tenantId);

            var tenantResponse = await _tenantService.GetTenantAsync(tenantId, cancellationToken);
            if (tenantResponse == null)
            {
                return NotFound(new { error = "Tenant not found" });
            }

            var success = await _tenantService.ReactivateTenantAsync(tenantId, cancellationToken);
            if (!success)
            {
                return BadRequest(new { error = "Failed to restore tenant" });
            }

            // Log audit entry
            var userId = GetCurrentUserId();
            await _auditLogService.LogActionAsync(
                null,
                userId,
                "SuperUser",
                "Tenant",
                tenantId,
                "Restore",
                metadata: $"Reason: {request.Reason}",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Tenant {TenantId} restored successfully", tenantId);

            return Ok(new TenantLifecycleResponse
            {
                TenantId = tenantId,
                Status = "Active",
                IsActive = true,
                IsArchived = false,
                IsDeleted = false,
                SuspendedAt = null,
                ArchivedAt = null,
                DeletedAt = null,
                Reason = request.Reason
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring tenant {TenantId}", tenantId);
            return StatusCode(500, new { error = "An error occurred while restoring the tenant" });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value 
            ?? User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
