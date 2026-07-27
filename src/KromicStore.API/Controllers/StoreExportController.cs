// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Store;

/// <summary>
/// Controller for store export, backup, and cloning operations.
/// </summary>
[ApiController]
[Route("api/v1/store/export")]
[Authorize(Roles = "TenantAdmin")]
[Produces("application/json")]
public class StoreExportController : BaseController
{
    private readonly IStoreExportService _exportService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<StoreExportController> _logger;

    public StoreExportController(
        ITenantProvider tenantProvider,
        IStoreExportService exportService,
        IAuditLogService auditLogService,
        ILogger<StoreExportController> logger)
        : base(tenantProvider)
    {
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Exports store data to JSON format.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The exported store data as JSON.</returns>
    /// <response code="200">Store exported successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="500">Server error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(StoreExportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportStore(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Exporting store for tenant {TenantId}", CurrentTenantId);

            var userId = GetCurrentUserId();
            var jsonData = await _exportService.ExportStoreAsync(CurrentTenantId, cancellationToken);

            // Log audit entry
            await _auditLogService.LogActionAsync(
                CurrentTenantId,
                userId,
                "User",
                "Store",
                CurrentTenantId,
                "Export",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Store export completed for tenant {TenantId}", CurrentTenantId);

            return Ok(new StoreExportResponse
            {
                TenantId = CurrentTenantId,
                ExportDate = DateTime.UtcNow,
                Data = jsonData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting store for tenant {TenantId}", CurrentTenantId);
            return StatusCode(500, new { error = "An error occurred while exporting the store" });
        }
    }

    /// <summary>
    /// Creates a backup snapshot of the store.
    /// </summary>
    /// <param name="request">The backup request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The backup ID.</returns>
    /// <response code="200">Backup created successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="500">Server error.</response>
    [HttpPost("backup")]
    [ProducesResponseType(typeof(CreateBackupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateBackup(
        [FromBody] CreateBackupRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating backup for tenant {TenantId}", CurrentTenantId);

            var userId = GetCurrentUserId();
            var backupId = await _exportService.CreateBackupAsync(
                CurrentTenantId,
                request.Description,
                cancellationToken);

            // Log audit entry
            await _auditLogService.LogActionAsync(
                CurrentTenantId,
                userId,
                "User",
                "StoreBackup",
                backupId,
                "Create",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Backup created for tenant {TenantId}: {BackupId}", CurrentTenantId, backupId);

            return Ok(new CreateBackupResponse
            {
                BackupId = backupId,
                TenantId = CurrentTenantId,
                CreatedAt = DateTime.UtcNow,
                Description = request.Description
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating backup for tenant {TenantId}", CurrentTenantId);
            return StatusCode(500, new { error = "An error occurred while creating the backup" });
        }
    }

    /// <summary>
    /// Restores a store from a backup.
    /// </summary>
    /// <param name="backupId">The backup ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Restore completed successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="404">Backup not found.</response>
    /// <response code="500">Server error.</response>
    [HttpPost("restore/{backupId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RestoreFromBackup(
        Guid backupId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Restoring tenant {TenantId} from backup {BackupId}",
                CurrentTenantId, backupId);

            var userId = GetCurrentUserId();
            await _exportService.RestoreFromBackupAsync(CurrentTenantId, backupId, cancellationToken);

            // Log audit entry
            await _auditLogService.LogActionAsync(
                CurrentTenantId,
                userId,
                "User",
                "StoreBackup",
                backupId,
                "Restore",
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Restore completed for tenant {TenantId} from backup {BackupId}",
                CurrentTenantId, backupId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring from backup {BackupId}", backupId);
            return StatusCode(500, new { error = "An error occurred while restoring from backup" });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value 
            ?? User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
