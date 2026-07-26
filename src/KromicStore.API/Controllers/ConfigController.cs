#nullable disable

namespace KromicStore.API.Controllers;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Configuration;
using KromicStore.Contracts.Abstractions;

/// <summary>
/// API controller for tenant-specific configuration management endpoints.
/// Accessible to TenantAdmin role.
/// </summary>
[ApiController]
[Route("api/v1/config")]
[Authorize]
[Produces("application/json")]
public class ConfigController : BaseController
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<ConfigController> _logger;

    /// <summary>
    /// Initializes a new instance of the ConfigController class.
    /// </summary>
    public ConfigController(
        ITenantProvider tenantProvider,
        IConfigurationService configurationService,
        ILogger<ConfigController> logger)
        : base(tenantProvider)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all tenant-specific configurations.
    /// </summary>
    /// <returns>Dictionary of tenant configurations.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IDictionary<string, string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllConfigurations(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("TenantAdmin retrieving configurations for tenant {TenantId}", CurrentTenantId);

            // Get all tenant configurations
            var configs = await _configurationService.GetSectionAsync(CurrentTenantId, string.Empty, cancellationToken);

            _logger.LogInformation("Retrieved {Count} configurations for tenant {TenantId}", configs.Count, CurrentTenantId);

            return Ok(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant configurations");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "CONFIG_RETRIEVAL_ERROR",
                Message = "Failed to retrieve configurations"
            });
        }
    }

    /// <summary>
    /// Gets a specific tenant configuration value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The configuration value.</returns>
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(TenantConfigurationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetConfiguration(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_KEY",
                Message = "Configuration key cannot be empty"
            });
        }

        try
        {
            _logger.LogInformation("TenantAdmin retrieving configuration {Key} for tenant {TenantId}", key, CurrentTenantId);

            var config = await _configurationService.GetAsync<string>(CurrentTenantId, key, cancellationToken: cancellationToken);

            if (config == null)
            {
                return NotFound(new ErrorResponse
                {
                    Code = "CONFIG_NOT_FOUND",
                    Message = $"Configuration '{key}' not found"
                });
            }

            return Ok(new TenantConfigurationResponse
            {
                ConfigKey = key,
                ConfigValue = config,
                Scope = "Tenant",
                IsEncrypted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant configuration {Key}", key);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "CONFIG_RETRIEVAL_ERROR",
                Message = "Failed to retrieve configuration"
            });
        }
    }

    /// <summary>
    /// Updates a tenant configuration value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="request">The configuration update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated configuration.</returns>
    [HttpPut("{key}")]
    [ProducesResponseType(typeof(TenantConfigurationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateConfiguration(
        string key,
        [FromBody] ConfigurationUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_KEY",
                Message = "Configuration key cannot be empty"
            });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Value))
        {
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_VALUE",
                Message = "Configuration value cannot be empty"
            });
        }

        // Check for restricted platform-wide configuration keys
        if (IsRestrictedKey(key))
        {
            return Forbid("This configuration key is restricted and cannot be modified by tenants");
        }

        try
        {
            _logger.LogInformation("TenantAdmin updating configuration {Key} for tenant {TenantId}", key, CurrentTenantId);

            var userId = GetCurrentUserId();

            await _configurationService.SetAsync(
                CurrentTenantId,
                key,
                request.Value,
                userId,
                request.Reason ?? "Updated by tenant admin",
                request.IsEncrypted,
                cancellationToken);

            return Ok(new TenantConfigurationResponse
            {
                ConfigKey = key,
                ConfigValue = request.Value,
                Scope = "Tenant",
                IsEncrypted = request.IsEncrypted,
                ExpiresAt = request.ExpiresAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant configuration {Key}", key);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "CONFIG_UPDATE_ERROR",
                Message = "Failed to update configuration"
            });
        }
    }

    /// <summary>
    /// Gets the configuration audit log for the tenant.
    /// </summary>
    /// <param name="from">Start date for filtering.</param>
    /// <param name="to">End date for filtering.</param>
    /// <param name="configKey">Optional configuration key filter.</param>
    /// <param name="userId">Optional user ID filter.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Audit log entries with pagination.</returns>
    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] DateTime from = default,
        [FromQuery] DateTime to = default,
        [FromQuery] string configKey = null,
        [FromQuery] Guid userId = default,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (take > 500)
            take = 500; // Limit maximum records per request

        if (skip < 0)
            skip = 0;

        if (take < 1)
            take = 1;

        try
        {
            _logger.LogInformation("TenantAdmin retrieving audit logs for tenant {TenantId}", CurrentTenantId);

            var (auditLogs, total) = await _configurationService.GetAuditLogAsync(
                CurrentTenantId,
                from,
                to,
                configKey,
                userId,
                skip,
                take,
                cancellationToken);

            return Ok(new
            {
                data = auditLogs,
                pagination = new
                {
                    skip,
                    take,
                    total,
                    hasMore = skip + take < total
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "AUDIT_LOG_ERROR",
                Message = "Failed to retrieve audit logs"
            });
        }
    }

    /// <summary>
    /// Resets a configuration to its default (platform-wide) value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success response.</returns>
    [HttpPost("reset-to-defaults/{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResetToDefaults(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_KEY",
                Message = "Configuration key cannot be empty"
            });
        }

        try
        {
            _logger.LogInformation("TenantAdmin resetting configuration {Key} for tenant {TenantId}", key, CurrentTenantId);

            var userId = GetCurrentUserId();

            await _configurationService.ResetAsync(
                CurrentTenantId,
                key,
                userId,
                cancellationToken);

            _logger.LogInformation("Configuration {Key} reset for tenant {TenantId}", key, CurrentTenantId);

            return Ok(new
            {
                message = $"Configuration '{key}' has been reset to default value",
                key,
                tenantId = CurrentTenantId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting configuration {Key}", key);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "CONFIG_RESET_ERROR",
                Message = "Failed to reset configuration"
            });
        }
    }

    /// <summary>
    /// Checks if a configuration key is restricted from tenant modification.
    /// </summary>
    private bool IsRestrictedKey(string key)
    {
        var restrictedPrefixes = new[] { "system:", "platform:", "billing:", "security:" };
        var lowerKey = key.ToLowerInvariant();
        return Array.Exists(restrictedPrefixes, prefix => lowerKey.StartsWith(prefix));
    }

    /// <summary>
    /// Exports configuration audit log as CSV file.
    /// </summary>
    /// <param name="from">Start date for filtering.</param>
    /// <param name="to">End date for filtering.</param>
    /// <param name="configKey">Optional configuration key filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>CSV file download.</returns>
    [HttpGet("audit-logs/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportAuditLog(
        [FromQuery] DateTime from = default,
        [FromQuery] DateTime to = default,
        [FromQuery] string configKey = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("TenantAdmin exporting audit logs for tenant {TenantId}", CurrentTenantId);

            var csv = await _configurationService.ExportAuditLogAsync(
                CurrentTenantId,
                from,
                to,
                configKey,
                cancellationToken);

            var fileName = $"config_audit_{CurrentTenantId:N}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

            return File(bytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit logs");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "EXPORT_ERROR",
                Message = "Failed to export audit logs"
            });
        }
    }

    /// <summary>
    /// Gets the current user ID from claims.
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value ?? User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        return !string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}


