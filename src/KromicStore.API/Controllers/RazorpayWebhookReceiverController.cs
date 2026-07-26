using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using KromicStore.Application.Interfaces;
using KromicStore.Infrastructure.Services.Webhooks;
using KromicStore.Domain.Entities;

namespace KromicStore.API.Controllers;

/// <summary>
/// Webhook receiver controller for Razorpay payment and subscription events.
/// These endpoints are public and do not require authentication.
/// </summary>
[ApiController]
[Route("api/webhooks/razorpay")]
[AllowAnonymous]
public class RazorpayWebhookReceiverController : ControllerBase
{
    private readonly IRazorpayService _razorpayService;
    private readonly RazorpaySubscriptionWebhookHandler _subscriptionWebhookHandler;
    private readonly RazorpayProductPaymentWebhookHandler _productPaymentWebhookHandler;
    private readonly ILogger<RazorpayWebhookReceiverController> _logger;

    /// <summary>
    /// Initializes a new instance of the RazorpayWebhookReceiverController.
    /// </summary>
    public RazorpayWebhookReceiverController(
        IRazorpayService razorpayService,
        RazorpaySubscriptionWebhookHandler subscriptionWebhookHandler,
        RazorpayProductPaymentWebhookHandler productPaymentWebhookHandler,
        ILogger<RazorpayWebhookReceiverController> logger)
    {
        _razorpayService = razorpayService ?? throw new ArgumentNullException(nameof(razorpayService));
        _subscriptionWebhookHandler = subscriptionWebhookHandler ?? throw new ArgumentNullException(nameof(subscriptionWebhookHandler));
        _productPaymentWebhookHandler = productPaymentWebhookHandler ?? throw new ArgumentNullException(nameof(productPaymentWebhookHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Receives and processes Razorpay subscription webhook events.
    /// Events include: subscription.charged, subscription.payment_failed, subscription.cancelled.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>200 OK if event processed successfully, 400 if invalid, 401 if signature verification fails.</returns>
    [HttpPost("subscriptions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ProcessSubscriptionWebhook(CancellationToken cancellationToken = default)
    {
        try
        {
            // Read raw request body as string
            string requestBody;
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                requestBody = await reader.ReadToEndAsync(cancellationToken);
            }

            // Validate request body is not empty
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                _logger.LogWarning("Received empty request body for subscription webhook");
                return BadRequest(new { error = "Request body is empty" });
            }

            // Parse JSON to extract event details
            JsonDocument? eventDoc;
            try
            {
                eventDoc = JsonDocument.Parse(requestBody);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Invalid JSON in subscription webhook request");
                return BadRequest(new { error = "Invalid JSON format in request body" });
            }

            var root = eventDoc.RootElement;

            // Extract event ID
            if (!root.TryGetProperty("id", out var eventIdElement))
            {
                _logger.LogWarning("Subscription webhook missing 'id' property");
                return BadRequest(new { error = "Event ID not found in webhook payload" });
            }

            var razorpayEventId = eventIdElement.GetString();
            if (string.IsNullOrWhiteSpace(razorpayEventId))
            {
                _logger.LogWarning("Subscription webhook event ID is empty");
                return BadRequest(new { error = "Event ID is empty" });
            }

            // Extract event type
            if (!root.TryGetProperty("event", out var eventTypeElement))
            {
                _logger.LogWarning("Subscription webhook missing 'event' property");
                return BadRequest(new { error = "Event type not found in webhook payload" });
            }

            var eventType = eventTypeElement.GetString();
            if (string.IsNullOrWhiteSpace(eventType))
            {
                _logger.LogWarning("Subscription webhook event type is empty");
                return BadRequest(new { error = "Event type is empty" });
            }

            // Get webhook secret from environment
            var webhookSecret = Environment.GetEnvironmentVariable("RAZORPAY_WEBHOOK_SECRET");
            if (string.IsNullOrWhiteSpace(webhookSecret))
            {
                _logger.LogError("RAZORPAY_WEBHOOK_SECRET environment variable is not configured");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Webhook secret not configured" });
            }

            // Create intermediate DTO for webhook parsing
            var subscriptionEventDto = new ParsedWebhookEventDto
            {
                RazorpayEventId = razorpayEventId,
                EventType = eventType,
                EventData = requestBody
            };

            // Handle subscription event via webhook handler
            var result = await _subscriptionWebhookHandler.HandleSubscriptionEventAsync(
                subscriptionEventDto,
                webhookSecret,
                cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Successfully processed subscription webhook event {EventId} of type {EventType}",
                    razorpayEventId,
                    eventType);
                return Ok(new { status = "success", eventId = razorpayEventId });
            }
            else
            {
                _logger.LogWarning(
                    "Subscription webhook processing failed - EventId: {EventId}, Error: {Error}",
                    razorpayEventId,
                    result.Error);
                return Unauthorized(new { error = result.Error, eventId = razorpayEventId });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing subscription webhook");
            return BadRequest(new { error = "An error occurred while processing the webhook" });
        }
    }

    /// <summary>
    /// Receives and processes Razorpay payment webhook events for one-time payments.
    /// Events include: payment.authorized, payment.failed, refund.created.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>200 OK if event processed successfully, 400 if invalid, 401 if signature verification fails.</returns>
    [HttpPost("payments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ProcessPaymentWebhook(CancellationToken cancellationToken = default)
    {
        try
        {
            // Read raw request body as string
            string requestBody;
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                requestBody = await reader.ReadToEndAsync(cancellationToken);
            }

            // Validate request body is not empty
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                _logger.LogWarning("Received empty request body for payment webhook");
                return BadRequest(new { error = "Request body is empty" });
            }

            // Get webhook secret from environment
            var webhookSecret = Environment.GetEnvironmentVariable("RAZORPAY_WEBHOOK_SECRET");
            if (string.IsNullOrWhiteSpace(webhookSecret))
            {
                _logger.LogError("RAZORPAY_WEBHOOK_SECRET environment variable is not configured");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Webhook secret not configured" });
            }

            // Handle payment event via webhook handler
            var result = await _productPaymentWebhookHandler.HandlePaymentEventAsync(
                requestBody,
                webhookSecret,
                cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Successfully processed payment webhook event");
                return Ok(new { status = "success" });
            }
            else
            {
                _logger.LogWarning("Payment webhook processing failed - Error: {Error}", result.Error);
                return Unauthorized(new { error = result.Error });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing payment webhook");
            return BadRequest(new { error = "An error occurred while processing the webhook" });
        }
    }
}

/// <summary>
/// Internal DTO for holding parsed webhook event data before processing.
/// </summary>
internal class ParsedWebhookEventDto : KromicStore.Infrastructure.Services.Webhooks.IWebhookEventData
{
    /// <summary>Gets or sets the unique Razorpay event ID.</summary>
    public string RazorpayEventId { get; set; } = string.Empty;

    /// <summary>Gets or sets the event type (e.g., "subscription.charged").</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Gets or sets the raw webhook event data as JSON.</summary>
    public string EventData { get; set; } = string.Empty;
}
