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
/// API controller for platform-wide configuration management endpoints.
/// Accessible only to SuperUser role.
/// </summary>
[ApiController]
[Route("api/v1/admin/config")]
[Authorize(Policy = "SuperUserOnly")]
[Produces("application/json")]
public class AdminConfigController : BaseController
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<AdminConfigController> _logger;

    /// <summary>
    /// Initializes a new instance of the AdminConfigController class.
    /// </summary>
    public AdminConfigController(
        ITenantProvider tenantProvider,
        IConfigurationService configurationService,
        ILogger<AdminConfigController> logger)
        : base(tenantProvider)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all platform configuration sections.
    /// </summary>
    /// <returns>Dictionary of all platform-wide configurations.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IDictionary<string, string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllConfigurations(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Admin retrieving all platform configurations");

            // Get all platform configurations
            var configs = await _configurationService.GetSectionAsync(null, string.Empty, cancellationToken);

            _logger.LogInformation("Retrieved {Count} platform configurations", configs.Count);

            return Ok(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving platform configurations");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "CONFIG_RETRIEVAL_ERROR",
                Message = "Failed to retrieve platform configurations"
            });
        }
    }

    /// <summary>
    /// Gets a specific platform configuration value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The configuration value.</returns>
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(SystemConfigurationResponse), StatusCodes.Status200OK)]
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
            _logger.LogInformation("Admin retrieving platform configuration {Key}", key);

            var config = await _configurationService.GetAsync<string>(null, key, cancellationToken: cancellationToken);

            if (config == null)
            {
                return NotFound(new ErrorResponse
                {
                    Code = "CONFIG_NOT_FOUND",
                    Message = $"Configuration '{key}' not found"
                });
            }

            return Ok(new SystemConfigurationResponse
            {
                ConfigKey = key,
                ConfigValue = config,
                IsEncrypted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving platform configuration {Key}", key);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "CONFIG_RETRIEVAL_ERROR",
                Message = "Failed to retrieve configuration"
            });
        }
    }

    /// <summary>
    /// Updates a platform configuration value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="request">The configuration update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated configuration.</returns>
    [HttpPut("{key}")]
    [ProducesResponseType(typeof(SystemConfigurationResponse), StatusCodes.Status200OK)]
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

        try
        {
            _logger.LogInformation("Admin updating platform configuration {Key}", key);

            var userId = GetCurrentUserId();

            await _configurationService.SetAsync(
                null, // Platform-wide config
                key,
                request.Value,
                userId,
                request.Reason ?? "Updated by superuser",
                request.IsEncrypted,
                cancellationToken);

            // Notify superusers of critical config changes
            if (request.Reason?.Contains("critical", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogWarning("Critical configuration {Key} updated by superuser {UserId}", key, userId);
            }

            return Ok(new SystemConfigurationResponse
            {
                ConfigKey = key,
                ConfigValue = request.Value,
                IsEncrypted = request.IsEncrypted,
                ExpiresAt = request.ExpiresAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating platform configuration {Key}", key);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "CONFIG_UPDATE_ERROR",
                Message = "Failed to update configuration"
            });
        }
    }

    /// <summary>
    /// Gets the configuration audit log with filtering options.
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
            _logger.LogInformation("Admin retrieving configuration audit logs");

            // Get audit logs for platform configs (tenantId = null for platform, but this gets tenant audit logs)
            // For platform configs, we'd need a different approach - for now, return empty list
            var (auditLogs, total) = await _configurationService.GetAuditLogAsync(
                null,
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
            _logger.LogInformation("Admin exporting audit logs");

            var csv = await _configurationService.ExportAuditLogAsync(
                null,
                from,
                to,
                configKey,
                cancellationToken);

            var fileName = $"config_audit_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
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


