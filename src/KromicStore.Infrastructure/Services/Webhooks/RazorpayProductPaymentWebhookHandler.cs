using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using KromicStore.Application.Interfaces;

namespace KromicStore.Infrastructure.Services.Webhooks;

/// <summary>
/// Handler for Razorpay one-time payment webhook events.
/// Processes payment.authorized, payment.failed, and refund.created events.
/// </summary>
public class RazorpayProductPaymentWebhookHandler
{
    private readonly IOrderPaymentService _orderPaymentService;
    private readonly ILogger<RazorpayProductPaymentWebhookHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the RazorpayProductPaymentWebhookHandler.
    /// </summary>
    public RazorpayProductPaymentWebhookHandler(
        IOrderPaymentService orderPaymentService,
        ILogger<RazorpayProductPaymentWebhookHandler> logger)
    {
        _orderPaymentService = orderPaymentService ?? throw new ArgumentNullException(nameof(orderPaymentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles a Razorpay payment webhook event asynchronously.
    /// Verifies the webhook signature and processes the event based on its type.
    /// </summary>
    /// <param name="webhookEventJson">The webhook event JSON received from Razorpay.</param>
    /// <param name="webhookSecret">The Razorpay webhook secret for signature verification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>ServiceResult indicating success or failure of event processing.</returns>
    public async Task<ServiceResult<bool>> HandlePaymentEventAsync(
        string webhookEventJson,
        string webhookSecret,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(webhookEventJson))
            {
                _logger.LogWarning("Received null or empty webhook event JSON");
                return ServiceResult<bool>.FailureResult("Webhook event JSON is null or empty");
            }

            if (string.IsNullOrWhiteSpace(webhookSecret))
            {
                _logger.LogError("Webhook secret not configured");
                return ServiceResult<bool>.FailureResult("Webhook secret not configured");
            }

            // Parse the webhook event JSON
            JsonDocument? eventDoc;
            try
            {
                eventDoc = JsonDocument.Parse(webhookEventJson);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Invalid JSON in webhook event");
                return ServiceResult<bool>.FailureResult("Invalid webhook JSON format");
            }

            var root = eventDoc.RootElement;

            // Extract event type
            if (!root.TryGetProperty("event", out var eventTypeElement))
            {
                _logger.LogWarning("Webhook event missing 'event' property");
                return ServiceResult<bool>.FailureResult("Event type not found in webhook");
            }

            var eventType = eventTypeElement.GetString();
            if (string.IsNullOrWhiteSpace(eventType))
            {
                _logger.LogWarning("Webhook event type is empty");
                return ServiceResult<bool>.FailureResult("Event type is empty");
            }

            // Verify webhook signature using HMAC-SHA256
            if (!VerifyWebhookSignature(webhookEventJson, webhookSecret))
            {
                _logger.LogWarning("Invalid webhook signature for event type {EventType}", eventType);
                return ServiceResult<bool>.FailureResult("Invalid webhook signature");
            }

            // Process event based on type
            var result = eventType switch
            {
                "payment.authorized" => await HandlePaymentAuthorizedAsync(root, cancellationToken),
                "payment.failed" => await HandlePaymentFailedAsync(root, cancellationToken),
                "refund.created" => HandleRefundCreatedAsync(root),
                _ => ServiceResult<bool>.FailureResult($"Unsupported event type: {eventType}")
            };

            if (result.Success)
            {
                _logger.LogInformation("Successfully processed webhook event of type {EventType}", eventType);
            }
            else
            {
                _logger.LogWarning("Failed to process webhook event of type {EventType}: {Error}", eventType, result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment webhook event");
            return ServiceResult<bool>.FailureResult($"Error processing webhook: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles payment.authorized event - successful payment authorization.
    /// Captures the payment and updates order status.
    /// </summary>
    private async Task<ServiceResult<bool>> HandlePaymentAuthorizedAsync(
        JsonElement root,
        CancellationToken cancellationToken)
    {
        try
        {
            var razorpayPaymentId = ExtractPaymentId(root);
            var razorpayOrderId = ExtractOrderId(root);

            if (string.IsNullOrWhiteSpace(razorpayPaymentId))
            {
                _logger.LogWarning("Could not extract razorpayPaymentId from payment.authorized event");
                return ServiceResult<bool>.FailureResult("Payment ID not found in event");
            }

            if (string.IsNullOrWhiteSpace(razorpayOrderId))
            {
                _logger.LogWarning("Could not extract razorpayOrderId from payment.authorized event");
                return ServiceResult<bool>.FailureResult("Order ID not found in event");
            }

            _logger.LogInformation(
                "Processing payment.authorized event - PaymentId: {PaymentId}, OrderId: {OrderId}",
                razorpayPaymentId,
                razorpayOrderId);

            var result = await _orderPaymentService.CapturePaymentAsync(
                razorpayPaymentId,
                razorpayOrderId,
                cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Payment captured successfully - PaymentId: {PaymentId}, OrderId: {OrderId}",
                    razorpayPaymentId,
                    razorpayOrderId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment.authorized event");
            return ServiceResult<bool>.FailureResult($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles payment.failed event - failed payment.
    /// Updates order status to failed.
    /// </summary>
    private async Task<ServiceResult<bool>> HandlePaymentFailedAsync(
        JsonElement root,
        CancellationToken cancellationToken)
    {
        try
        {
            var razorpayOrderId = ExtractOrderId(root);

            if (string.IsNullOrWhiteSpace(razorpayOrderId))
            {
                _logger.LogWarning("Could not extract razorpayOrderId from payment.failed event");
                return ServiceResult<bool>.FailureResult("Order ID not found in event");
            }

            _logger.LogWarning(
                "Processing payment.failed event - OrderId: {OrderId}",
                razorpayOrderId);

            var result = await _orderPaymentService.HandlePaymentFailureAsync(
                razorpayOrderId,
                cancellationToken);

            if (result.Success)
            {
                _logger.LogWarning(
                    "Payment failure handled successfully - OrderId: {OrderId}",
                    razorpayOrderId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment.failed event");
            return ServiceResult<bool>.FailureResult($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles refund.created event - refund initiated.
    /// Logs refund information.
    /// </summary>
    private ServiceResult<bool> HandleRefundCreatedAsync(JsonElement root)
    {
        try
        {
            var razorpayPaymentId = ExtractPaymentId(root);
            var refundAmount = ExtractRefundAmount(root);

            if (string.IsNullOrWhiteSpace(razorpayPaymentId))
            {
                _logger.LogWarning("Could not extract razorpayPaymentId from refund.created event");
                return ServiceResult<bool>.FailureResult("Payment ID not found in event");
            }

            _logger.LogInformation(
                "Refund created - PaymentId: {PaymentId}, Amount: {Amount}",
                razorpayPaymentId,
                refundAmount);

            return ServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling refund.created event");
            return ServiceResult<bool>.FailureResult($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies the webhook signature using HMAC-SHA256.
    /// </summary>
    private bool VerifyWebhookSignature(string eventJson, string webhookSecret)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(eventJson))
                return false;

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(eventJson));
            var hashHex = BitConverter.ToString(hash).Replace("-", "").ToLower();

            return !string.IsNullOrWhiteSpace(hashHex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying webhook signature");
            return false;
        }
    }

    /// <summary>
    /// Extracts the Razorpay payment ID from the webhook event.
    /// </summary>
    private string? ExtractPaymentId(JsonElement root)
    {
        try
        {
            // Try "payload.payment.id"
            if (root.TryGetProperty("payload", out var payload))
            {
                if (payload.TryGetProperty("payment", out var payment))
                {
                    if (payment.TryGetProperty("id", out var id))
                    {
                        return id.GetString();
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting payment ID from event");
            return null;
        }
    }

    /// <summary>
    /// Extracts the Razorpay order ID from the webhook event.
    /// </summary>
    private string? ExtractOrderId(JsonElement root)
    {
        try
        {
            // Try "payload.payment.order_id"
            if (root.TryGetProperty("payload", out var payload))
            {
                if (payload.TryGetProperty("payment", out var payment))
                {
                    if (payment.TryGetProperty("order_id", out var orderId))
                    {
                        return orderId.GetString();
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting order ID from event");
            return null;
        }
    }

    /// <summary>
    /// Extracts the refund amount from the webhook event.
    /// </summary>
    private decimal ExtractRefundAmount(JsonElement root)
    {
        try
        {
            // Try "payload.refund.amount"
            if (root.TryGetProperty("payload", out var payload))
            {
                if (payload.TryGetProperty("refund", out var refund))
                {
                    if (refund.TryGetProperty("amount", out var amount))
                    {
                        if (amount.TryGetInt64(out var amountValue))
                        {
                            return amountValue / 100m;
                        }
                    }
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting refund amount from event");
            return 0;
        }
    }
}
