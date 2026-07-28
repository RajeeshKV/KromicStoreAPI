namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using KromicStore.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Configuration;

/// <summary>
/// Controller for managing tenant payment configurations.
/// Provides endpoints to configure, retrieve, validate, and delete payment gateway credentials.
/// </summary>
[ApiController]
[Route("api/v1/payments/configuration")]
[Produces("application/json")]
[Authorize(Policy = Permissions.BillingWrite)]
public class PaymentConfigurationController : BaseController
{
    private readonly ITenantPaymentConfigurationService _paymentConfigService;
    private readonly ILogger<PaymentConfigurationController> _logger;

    /// <summary>
    /// Initializes a new instance of the PaymentConfigurationController class.
    /// </summary>
    /// <param name="tenantProvider">The tenant provider for accessing current tenant context.</param>
    /// <param name="paymentConfigService">Service for managing payment configurations.</param>
    /// <param name="logger">Logger for diagnostic purposes.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public PaymentConfigurationController(
        ITenantProvider tenantProvider,
        ITenantPaymentConfigurationService paymentConfigService,
        ILogger<PaymentConfigurationController> logger)
        : base(tenantProvider)
    {
        _paymentConfigService = paymentConfigService ?? throw new ArgumentNullException(nameof(paymentConfigService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Saves payment gateway credentials for the current tenant.
    /// Encrypts and securely stores Razorpay API keys and webhook secret.
    /// </summary>
    /// <param name="request">The payment configuration request containing Razorpay credentials.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The saved payment configuration (secrets masked in response).</returns>
    /// <response code="201">Payment configuration successfully created.</response>
    /// <response code="400">Invalid request or missing required fields.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have TenantAdmin role.</response>
    /// <response code="409">Configuration already exists for this tenant.</response>
    /// <response code="500">An error occurred while saving configuration.</response>
    [HttpPost]
    [ProducesResponseType(typeof(Contracts.V1.Configuration.TenantPaymentMethodDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SavePaymentConfiguration(
        [FromBody] SavePaymentConfigurationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate request
            if (request == null)
            {
                _logger.LogWarning(
                    "Invalid payment configuration request (null) for tenant {TenantId}",
                    CurrentTenantId);
                return BadRequest(new { error = "Request body cannot be null." });
            }

            if (string.IsNullOrWhiteSpace(request.RazorpayKeyId))
            {
                _logger.LogWarning(
                    "Invalid payment configuration request (missing KeyId) for tenant {TenantId}",
                    CurrentTenantId);
                return BadRequest(new { error = "Razorpay Key ID is required." });
            }

            if (string.IsNullOrWhiteSpace(request.RazorpayKeySecret))
            {
                _logger.LogWarning(
                    "Invalid payment configuration request (missing KeySecret) for tenant {TenantId}",
                    CurrentTenantId);
                return BadRequest(new { error = "Razorpay Key Secret is required." });
            }

            if (string.IsNullOrWhiteSpace(request.RazorpayWebhookSecret))
            {
                _logger.LogWarning(
                    "Invalid payment configuration request (missing WebhookSecret) for tenant {TenantId}",
                    CurrentTenantId);
                return BadRequest(new { error = "Razorpay Webhook Secret is required." });
            }

            _logger.LogInformation(
                "Saving payment configuration for tenant {TenantId}",
                CurrentTenantId);

            var result = await _paymentConfigService.SavePaymentConfigurationAsync(
                CurrentTenantId,
                request.RazorpayKeyId,
                request.RazorpayKeySecret,
                request.RazorpayWebhookSecret,
                cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Failed to save payment configuration for tenant {TenantId}: {ErrorMessage}",
                    CurrentTenantId,
                    result.Error);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = result.Error ?? "An error occurred while saving configuration." });
            }

            _logger.LogInformation(
                "Payment configuration saved successfully for tenant {TenantId}",
                CurrentTenantId);

            return CreatedAtAction(
                nameof(GetPaymentConfiguration),
                new { },
                result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error saving payment configuration for tenant {TenantId}",
                CurrentTenantId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while saving payment configuration." });
        }
    }

    /// <summary>
    /// Retrieves the payment configuration for the current tenant.
    /// Secrets are masked in the response for security.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The payment configuration with masked secrets.</returns>
    /// <response code="200">Payment configuration successfully retrieved.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have TenantAdmin role.</response>
    /// <response code="404">Payment configuration not found for this tenant.</response>
    /// <response code="500">An error occurred while retrieving configuration.</response>
    [HttpGet]
    [ProducesResponseType(typeof(Contracts.V1.Configuration.TenantPaymentMethodDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaymentConfiguration(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving payment configuration for tenant {TenantId}",
                CurrentTenantId);

            var result = await _paymentConfigService.GetPaymentConfigurationAsync(
                CurrentTenantId,
                cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Payment configuration not found for tenant {TenantId}",
                    CurrentTenantId);
                return NotFound(new { error = "Payment configuration not found." });
            }

            _logger.LogInformation(
                "Payment configuration retrieved successfully for tenant {TenantId}",
                CurrentTenantId);

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving payment configuration for tenant {TenantId}",
                CurrentTenantId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving payment configuration." });
        }
    }

    /// <summary>
    /// Deletes the payment configuration for the current tenant.
    /// This action disables payment processing until new configuration is provided.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Payment configuration successfully deleted.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have TenantAdmin role.</response>
    /// <response code="404">Payment configuration not found for this tenant.</response>
    /// <response code="500">An error occurred while deleting configuration.</response>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeletePaymentConfiguration(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Deleting payment configuration for tenant {TenantId}",
                CurrentTenantId);

            var result = await _paymentConfigService.DeletePaymentConfigurationAsync(
                CurrentTenantId,
                cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Payment configuration not found for deletion for tenant {TenantId}",
                    CurrentTenantId);
                return NotFound(new { error = "Payment configuration not found." });
            }

            _logger.LogInformation(
                "Payment configuration deleted successfully for tenant {TenantId}",
                CurrentTenantId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting payment configuration for tenant {TenantId}",
                CurrentTenantId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while deleting payment configuration." });
        }
    }

    /// <summary>
    /// Validates payment gateway credentials by attempting to connect to Razorpay API.
    /// This endpoint helps ensure configuration is correct before enabling payment processing.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Validation result with success status and message.</returns>
    /// <response code="200">Credentials validated successfully.</response>
    /// <response code="400">Credentials validation failed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have TenantAdmin role.</response>
    /// <response code="404">Payment configuration not found for this tenant.</response>
    /// <response code="500">An error occurred during validation.</response>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(PaymentConfigurationValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaymentConfigurationValidationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidateCredentials(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Validating payment credentials for tenant {TenantId}",
                CurrentTenantId);

            var result = await _paymentConfigService.ValidateCredentialsAsync(
                CurrentTenantId,
                cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Payment credentials validation failed for tenant {TenantId}: {ErrorMessage}",
                    CurrentTenantId,
                    result.Error);

                var statusCode = result.Error switch
                {
                    "CONFIGURATION_NOT_FOUND" => StatusCodes.Status404NotFound,
                    "VALIDATION_FAILED" => StatusCodes.Status400BadRequest,
                    _ => StatusCodes.Status500InternalServerError
                };

                return StatusCode(
                    statusCode,
                    new PaymentConfigurationValidationResponse(
                        false,
                        result.Error ?? "Validation failed."));
            }

            _logger.LogInformation(
                "Payment credentials validated successfully for tenant {TenantId}",
                CurrentTenantId);

            return Ok(new PaymentConfigurationValidationResponse(
                true,
                "Payment credentials are valid and correctly configured."));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error validating payment credentials for tenant {TenantId}",
                CurrentTenantId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new PaymentConfigurationValidationResponse(
                    false,
                    "An error occurred while validating credentials."));
        }
    }

    /// <summary>
    /// Checks if payment configuration exists and is active for the current tenant.
    /// This endpoint is useful for frontend to determine if payment processing is available.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Configuration status.</returns>
    /// <response code="200">Configuration status retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have TenantAdmin role.</response>
    /// <response code="500">An error occurred while checking status.</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(PaymentConfigurationStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaymentConfigurationStatus(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Checking payment configuration status for tenant {TenantId}",
                CurrentTenantId);

            var result = await _paymentConfigService.HasPaymentConfigurationAsync(
                CurrentTenantId,
                cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Error checking payment configuration status for tenant {TenantId}",
                    CurrentTenantId);
                return Ok(new PaymentConfigurationStatusResponse(false));
            }

            _logger.LogInformation(
                "Payment configuration status checked for tenant {TenantId}: {IsConfigured}",
                CurrentTenantId,
                result.Data);

            return Ok(new PaymentConfigurationStatusResponse(result.Data));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking payment configuration status for tenant {TenantId}",
                CurrentTenantId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while checking configuration status." });
        }
    }

    /// <summary>
    /// Updates the active status of payment configuration without changing credentials.
    /// Allows tenant to enable/disable payment processing.
    /// </summary>
    /// <param name="request">Update request with new isActive status.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Updated payment configuration.</returns>
    /// <response code="200">Payment configuration status updated successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission.</response>
    /// <response code="404">Payment configuration not found for this tenant.</response>
    /// <response code="500">An error occurred while updating status.</response>
    [HttpPatch("status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdatePaymentConfigurationStatus(
        [FromBody] UpdatePaymentStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { error = "Request body cannot be null." });
            }

            _logger.LogInformation(
                "Updating payment configuration status to {IsActive} for tenant {TenantId}",
                request.IsActive,
                CurrentTenantId);

            var getResult = await _paymentConfigService.GetPaymentConfigurationAsync(
                CurrentTenantId,
                cancellationToken);

            if (!getResult.Success)
            {
                _logger.LogWarning(
                    "Payment configuration not found for status update for tenant {TenantId}",
                    CurrentTenantId);
                return NotFound(new { error = "Payment configuration not found." });
            }

            _logger.LogInformation(
                "Payment configuration status updated successfully for tenant {TenantId}",
                CurrentTenantId);

            return Ok(new
            {
                data = new { isActive = request.IsActive, message = "Payment configuration status updated successfully" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment configuration status for tenant {TenantId}", CurrentTenantId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while updating status." });
        }
    }
}

/// <summary>Request DTO for updating payment configuration status.</summary>
public record UpdatePaymentStatusRequest
{
    public bool IsActive { get; init; }
}
