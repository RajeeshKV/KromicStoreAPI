namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using KromicStore.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;
using KromicStore.Domain.Entities;

/// <summary>
/// Controller for managing Razorpay payment gateway configurations.
/// </summary>
[ApiController]
[Route("api/v1/payment/razorpay")]
[Produces("application/json")]
[Authorize(Policy = Permissions.SettingsWrite)]
public class RazorpayConfigurationController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RazorpayConfigurationController> _logger;

    /// <summary>
    /// Initializes a new instance of the RazorpayConfigurationController class.
    /// </summary>
    public RazorpayConfigurationController(
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork,
        ILogger<RazorpayConfigurationController> logger)
        : base(tenantProvider)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the Razorpay configuration for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Razorpay configuration (without secret).</returns>
    [HttpGet]
    [ProducesResponseType(typeof(RazorpayConfigurationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetConfiguration(CancellationToken cancellationToken = default)
    {
        try
        {
            var config = (await _unitOfWork.RazorpayConfigurations.FindAsync(
                r => r.TenantId == CurrentTenantId,
                cancellationToken)).FirstOrDefault();

            if (config == null)
            {
                return NotFound(new { error = "Razorpay configuration not found." });
            }

            var response = new RazorpayConfigurationResponse
            {
                Id = config.Id,
                KeyId = config.KeyId,
                Environment = config.Environment,
                IsActive = config.IsActive,
                Description = config.Description,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Razorpay configuration for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving the configuration." });
        }
    }

    /// <summary>
    /// Creates or updates the Razorpay configuration for the current tenant.
    /// </summary>
    /// <param name="request">The Razorpay configuration request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created or updated configuration (without secret).</returns>
    [HttpPost]
    [ProducesResponseType(typeof(RazorpayConfigurationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpsertConfiguration(
        [FromBody] UpsertRazorpayConfigurationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.KeyId) || string.IsNullOrWhiteSpace(request.KeySecret))
            {
                return BadRequest(new { error = "Key ID and Key Secret are required." });
            }

            var existingConfig = (await _unitOfWork.RazorpayConfigurations.FindAsync(
                r => r.TenantId == CurrentTenantId,
                cancellationToken)).FirstOrDefault();

            RazorpayConfiguration config;

            if (existingConfig != null)
            {
                existingConfig.UpdateConfig(
                    request.KeyId,
                    request.KeySecret,
                    request.Environment,
                    request.WebhookSecret,
                    request.Description
                );

                if (request.IsActive.HasValue)
                {
                    if (request.IsActive.Value)
                        existingConfig.Activate();
                    else
                        existingConfig.Deactivate();
                }

                config = existingConfig;
                _logger.LogInformation("Updated Razorpay configuration for tenant {TenantId}", CurrentTenantId);
            }
            else
            {
                config = RazorpayConfiguration.Create(
                    CurrentTenantId,
                    request.KeyId,
                    request.KeySecret,
                    request.Environment
                );

                if (request.Description != null)
                {
                    config.UpdateConfig(
                        request.KeyId,
                        request.KeySecret,
                        request.Environment,
                        request.WebhookSecret,
                        request.Description
                    );
                }

                if (request.IsActive.HasValue && !request.IsActive.Value)
                {
                    config.Deactivate();
                }

                await _unitOfWork.RazorpayConfigurations.AddAsync(config, cancellationToken);
                _logger.LogInformation("Created Razorpay configuration for tenant {TenantId}", CurrentTenantId);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new RazorpayConfigurationResponse
            {
                Id = config.Id,
                KeyId = config.KeyId,
                Environment = config.Environment,
                IsActive = config.IsActive,
                Description = config.Description,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting Razorpay configuration for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while saving the configuration." });
        }
    }

    /// <summary>
    /// Activates or deactivates the Razorpay configuration.
    /// </summary>
    /// <param name="request">The activation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated configuration (without secret).</returns>
    [HttpPatch("status")]
    [ProducesResponseType(typeof(RazorpayConfigurationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateStatus(
        [FromBody] UpdateRazorpayStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = (await _unitOfWork.RazorpayConfigurations.FindAsync(
                r => r.TenantId == CurrentTenantId,
                cancellationToken)).FirstOrDefault();

            if (config == null)
            {
                return NotFound(new { error = "Razorpay configuration not found." });
            }

            if (request.IsActive)
            {
                config.Activate();
            }
            else
            {
                config.Deactivate();
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated Razorpay configuration status for tenant {TenantId}", CurrentTenantId);

            var response = new RazorpayConfigurationResponse
            {
                Id = config.Id,
                KeyId = config.KeyId,
                Environment = config.Environment,
                IsActive = config.IsActive,
                Description = config.Description,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Razorpay configuration status for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while updating the status." });
        }
    }

    /// <summary>
    /// Deletes the Razorpay configuration for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteConfiguration(CancellationToken cancellationToken = default)
    {
        try
        {
            var config = (await _unitOfWork.RazorpayConfigurations.FindAsync(
                r => r.TenantId == CurrentTenantId,
                cancellationToken)).FirstOrDefault();

            if (config == null)
            {
                return NotFound(new { error = "Razorpay configuration not found." });
            }

            _unitOfWork.RazorpayConfigurations.Delete(config);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted Razorpay configuration for tenant {TenantId}", CurrentTenantId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Razorpay configuration for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while deleting the configuration." });
        }
    }
}

/// <summary>
/// Response model for Razorpay configuration (without secret).
/// </summary>
public class RazorpayConfigurationResponse
{
    public Guid Id { get; set; }
    public string KeyId { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request model for upserting Razorpay configuration.
/// </summary>
public class UpsertRazorpayConfigurationRequest
{
    public string KeyId { get; set; } = string.Empty;
    public string KeySecret { get; set; } = string.Empty;
    public string Environment { get; set; } = "Test";
    public string? WebhookSecret { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Request model for updating Razorpay configuration status.
/// </summary>
public class UpdateRazorpayStatusRequest
{
    public bool IsActive { get; set; }
}
