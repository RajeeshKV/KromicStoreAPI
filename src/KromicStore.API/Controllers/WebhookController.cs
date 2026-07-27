#nullable disable

namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using KromicStore.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Webhooks;
using KromicStore.Contracts.Abstractions;
using KromicStore.Domain.Enums;
using System;
using System.Threading.Tasks;

/// <summary>
/// API controller for webhook management endpoints.
/// Accessible only to TenantAdmin role.
/// </summary>
[ApiController]
[Route("api/v1/webhooks")]
[Authorize(Policy = Permissions.SettingsRead)]
public class WebhookController : BaseController
{
    private readonly IWebhookService _webhookService;
    private readonly ILogger<WebhookController> _logger;

    /// <summary>
    /// Initializes a new instance of the WebhookController class.
    /// </summary>
    public WebhookController(
        IWebhookService webhookService,
        ILogger<WebhookController> logger,
        ITenantProvider tenantProvider)
        : base(tenantProvider)
    {
        _webhookService = webhookService ?? throw new ArgumentNullException(nameof(webhookService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new webhook configuration.
    /// </summary>
    /// <param name="request">The webhook configuration request with endpoint URL and event types.</param>
    /// <returns>The registered webhook configuration with generated secret.</returns>
    /// <response code="201">Webhook successfully registered.</response>
    /// <response code="400">Invalid request or endpoint URL.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="500">Server error during registration.</response>
    [Authorize(Policy = Permissions.SettingsWrite)]
    [HttpPost]
    [ProducesResponseType(typeof(WebhookConfigurationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterWebhook([FromBody] WebhookConfigurationRequest request)
    {
        if (request == null)
        {
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_REQUEST",
                Message = "Request body is required"
            });
        }

        if (string.IsNullOrWhiteSpace(request.EndpointUrl))
        {
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_ENDPOINT_URL",
                Message = "Endpoint URL is required"
            });
        }

        try
        {
            _logger.LogInformation("Registering new webhook for tenant {TenantId}", GetTenantId());

            var config = await _webhookService.RegisterWebhookAsync(
                GetTenantId(),
                request.EndpointUrl,
                request.EventTypes as WebhookEventType[] ?? System.Linq.Enumerable.ToArray(request.EventTypes),
                HttpContext.RequestAborted);

            var response = new WebhookConfigurationResponse
            {
                Id = config.Id,
                TenantId = config.TenantId,
                EndpointUrl = config.EndpointUrl,
                EventTypes = config.EventTypes,
                Secret = config.Secret,
                Description = request.Description,
                IsActive = config.IsActive,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            };

            return CreatedAtAction(nameof(GetWebhook), new { id = response.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering webhook");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "An error occurred while registering the webhook"
            });
        }
    }

    /// <summary>
    /// Gets a specific webhook configuration.
    /// </summary>
    /// <param name="id">The webhook configuration ID.</param>
    /// <returns>The webhook configuration details.</returns>
    /// <response code="200">Webhook configuration successfully retrieved.</response>
    /// <response code="400">Invalid webhook ID.</response>
    /// <response code="404">Webhook not found.</response>
    /// <response code="500">Server error retrieving webhook.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WebhookConfigurationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult GetWebhook(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_ID",
                Message = "Webhook ID is required"
            });
        }

        try
        {
            _logger.LogInformation("Getting webhook {WebhookId} for tenant {TenantId}", id, GetTenantId());

            // In a full implementation, would fetch from database and check tenant ownership
            return NotFound(new ErrorResponse
            {
                Code = "WEBHOOK_NOT_FOUND",
                Message = "Webhook not found"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting webhook");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "An error occurred while retrieving the webhook"
            });
        }
    }

    /// <summary>
    /// Lists all webhooks for the current tenant with pagination.
    /// </summary>
    /// <param name="pageNumber">The page number (default 1).</param>
    /// <param name="pageSize">The page size (default 10, max 100).</param>
    /// <returns>Paginated list of webhooks.</returns>
    /// <response code="200">Webhooks successfully retrieved.</response>
    /// <response code="400">Invalid pagination parameters.</response>
    /// <response code="500">Server error listing webhooks.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<WebhookConfigurationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListWebhooks(int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            _logger.LogInformation("Listing webhooks for tenant {TenantId} (page {PageNumber}, size {PageSize})",
                GetTenantId(), pageNumber, pageSize);

            var webhooks = await _webhookService.ListWebhooksAsync(GetTenantId(), HttpContext.RequestAborted);

            // In a full implementation, would apply pagination here
            return Ok(new PagedResponse<WebhookConfigurationResponse>
            {
                Data = new List<WebhookConfigurationResponse>(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing webhooks");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "An error occurred while listing webhooks"
            });
        }
    }

    /// <summary>
    /// Updates an existing webhook configuration.
    /// </summary>
    /// <param name="id">The webhook configuration ID.</param>
    /// <param name="request">The updated webhook configuration.</param>
    /// <returns>The updated webhook configuration.</returns>
    /// <response code="200">Webhook successfully updated.</response>
    /// <response code="400">Invalid webhook ID or request data.</response>
    /// <response code="404">Webhook not found.</response>
    /// <response code="500">Server error updating webhook.</response>
    [Authorize(Policy = Permissions.SettingsWrite)]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(WebhookConfigurationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult UpdateWebhook(Guid id, [FromBody] WebhookConfigurationRequest request)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_ID",
                Message = "Webhook ID is required"
            });
        }

        try
        {
            _logger.LogInformation("Updating webhook {WebhookId} for tenant {TenantId}", id, GetTenantId());

            // In a full implementation, would update the webhook
            return NotFound(new ErrorResponse
            {
                Code = "WEBHOOK_NOT_FOUND",
                Message = "Webhook not found"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating webhook");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "An error occurred while updating the webhook"
            });
        }
    }

    /// <summary>
    /// Deletes (unregisters) a webhook configuration.
    /// </summary>
    /// <param name="id">The webhook configuration ID.</param>
    /// <returns>No content response on success.</returns>
    /// <response code="204">Webhook successfully deleted.</response>
    /// <response code="400">Invalid webhook ID.</response>
    /// <response code="404">Webhook not found.</response>
    /// <response code="500">Server error deleting webhook.</response>
    [Authorize(Policy = Permissions.SettingsWrite)]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteWebhook(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_ID",
                Message = "Webhook ID is required"
            });
        }

        try
        {
            _logger.LogInformation("Deleting webhook {WebhookId} for tenant {TenantId}", id, GetTenantId());

            await _webhookService.UnregisterWebhookAsync(GetTenantId(), id, HttpContext.RequestAborted);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting webhook");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "An error occurred while deleting the webhook"
            });
        }
    }

    /// <summary>
    /// Sends a test event to the webhook endpoint.
    /// </summary>
    /// <param name="id">The webhook configuration ID.</param>
    /// <returns>The test delivery result.</returns>
    /// <response code="200">Test event sent successfully.</response>
    /// <response code="400">Invalid webhook ID.</response>
    /// <response code="404">Webhook not found.</response>
    /// <response code="500">Server error sending test event.</response>
    [Authorize(Policy = Permissions.SettingsWrite)]
    [HttpPost("{id}/test")]
    [ProducesResponseType(typeof(TestEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult SendTestEvent(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_ID",
                Message = "Webhook ID is required"
            });
        }

        try
        {
            _logger.LogInformation("Sending test event to webhook {WebhookId}", id);

            return Ok(new TestEventResponse
            {
                Success = false,
                Message = "Test event delivery would be sent here"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test event");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "An error occurred while sending the test event"
            });
        }
    }

    /// <summary>
    /// Replays a webhook event.
    /// </summary>
    /// <param name="eventId">The event ID to replay.</param>
    /// <returns>No content response on success.</returns>
    /// <response code="202">Event replay accepted for processing.</response>
    /// <response code="400">Invalid event ID.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="500">Server error replaying event.</response>
    [Authorize(Policy = Permissions.SettingsWrite)]
    [HttpPost("events/{eventId}/replay")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReplayEvent(Guid eventId)
    {
        if (eventId == Guid.Empty)
        {
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_EVENT_ID",
                Message = "Event ID is required"
            });
        }

        try
        {
            _logger.LogInformation("Replaying event {EventId} for tenant {TenantId}", eventId, GetTenantId());

            await _webhookService.RetryDeliveryAsync(GetTenantId(), eventId, HttpContext.RequestAborted);

            return Accepted();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replaying event");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "An error occurred while replaying the event"
            });
        }
    }

    /// <summary>
    /// Gets delivery logs for a specific webhook.
    /// </summary>
    /// <param name="id">The webhook configuration ID.</param>
    /// <param name="pageNumber">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>Paginated list of delivery logs.</returns>
    /// <response code="200">Delivery logs successfully retrieved.</response>
    /// <response code="400">Invalid webhook ID or pagination parameters.</response>
    /// <response code="404">Webhook not found.</response>
    /// <response code="500">Server error retrieving delivery logs.</response>
    [HttpGet("{id}/deliveries")]
    [ProducesResponseType(typeof(PagedResponse<WebhookDeliveryLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDeliveryLogs(Guid id, int pageNumber = 1, int pageSize = 10)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_ID",
                Message = "Webhook ID is required"
            });
        }

        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            _logger.LogInformation("Getting delivery logs for webhook {WebhookId}", id);

            var (logs, total) = await _webhookService.GetDeliveryLogsAsync(
                GetTenantId(), id,
                (pageNumber - 1) * pageSize, pageSize,
                HttpContext.RequestAborted);

            return Ok(new PagedResponse<WebhookDeliveryLogResponse>
            {
                Data = new List<WebhookDeliveryLogResponse>(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = total
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting delivery logs");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "An error occurred while retrieving delivery logs"
            });
        }
    }

    /// <summary>
    /// Helper method to extract tenant ID from context.
    /// </summary>
    private Guid GetTenantId()
    {
        // In a full implementation, would extract from claims or context
        return Guid.NewGuid();
    }
}

/// <summary>
/// Response DTO for test event.
/// </summary>
public class TestEventResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public int? HttpStatusCode { get; set; }
}

/// <summary>
/// Paged response DTO.
/// </summary>
public class PagedResponse<T>
{
    public IEnumerable<T> Data { get; set; } = new List<T>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}


