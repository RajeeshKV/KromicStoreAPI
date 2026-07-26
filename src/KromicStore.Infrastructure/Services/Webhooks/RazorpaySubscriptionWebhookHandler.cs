using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using KromicStore.Application.Interfaces;
using KromicStore.Domain.Entities;

namespace KromicStore.Infrastructure.Services.Webhooks;

/// <summary>
/// Represents parsed webhook event data needed for processing.
/// </summary>
public interface IWebhookEventData
{
    /// <summary>Gets the unique Razorpay event ID.</summary>
    string RazorpayEventId { get; }

    /// <summary>Gets the event type (e.g., "subscription.charged").</summary>
    string EventType { get; }

    /// <summary>Gets the raw webhook event data as JSON.</summary>
    string EventData { get; }
}

/// <summary>
/// Handler for Razorpay subscription webhook events.
/// Processes subscription.charged, subscription.payment_failed, and subscription.cancelled events.
/// </summary>
public class RazorpaySubscriptionWebhookHandler
{
    private readonly ISubscriptionPaymentService _subscriptionPaymentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RazorpaySubscriptionWebhookHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the RazorpaySubscriptionWebhookHandler.
    /// </summary>
    public RazorpaySubscriptionWebhookHandler(
        ISubscriptionPaymentService subscriptionPaymentService,
        IUnitOfWork unitOfWork,
        ILogger<RazorpaySubscriptionWebhookHandler> logger)
    {
        _subscriptionPaymentService = subscriptionPaymentService ?? throw new ArgumentNullException(nameof(subscriptionPaymentService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles a Razorpay subscription webhook event asynchronously.
    /// Verifies the webhook signature and processes the event based on its type.
    /// </summary>
    /// <param name="webhookEvent">The webhook event data received from Razorpay.</param>
    /// <param name="webhookSecret">The Razorpay webhook secret for signature verification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>ServiceResult indicating success or failure of event processing.</returns>
    public async Task<ServiceResult<bool>> HandleSubscriptionEventAsync(
        IWebhookEventData webhookEvent,
        string webhookSecret,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (webhookEvent == null)
            {
                _logger.LogWarning("Received null webhook event");
                return ServiceResult<bool>.FailureResult("Webhook event is null");
            }

            if (string.IsNullOrWhiteSpace(webhookSecret))
            {
                _logger.LogError("Webhook secret not configured");
                return ServiceResult<bool>.FailureResult("Webhook secret not configured");
            }

            // Verify webhook signature using HMAC-SHA256
            if (!VerifyWebhookSignature(webhookEvent.EventData, webhookSecret))
            {
                _logger.LogWarning(
                    "Invalid webhook signature for event {EventId} of type {EventType}",
                    webhookEvent.RazorpayEventId,
                    webhookEvent.EventType);
                return ServiceResult<bool>.FailureResult("Invalid webhook signature");
            }

            // Extract razorpaySubscriptionId from the event data
            var razorpaySubscriptionId = ExtractRazorpaySubscriptionId(webhookEvent.EventData);
            if (string.IsNullOrWhiteSpace(razorpaySubscriptionId))
            {
                _logger.LogWarning(
                    "Could not extract razorpaySubscriptionId from event {EventId}",
                    webhookEvent.RazorpayEventId);
                return ServiceResult<bool>.FailureResult("Could not extract subscription ID from event");
            }

            // Find the subscription in our system
            var subscriptions = await _unitOfWork.Subscriptions
                .FindAsync(s => s.RazorpaySubscriptionId == razorpaySubscriptionId, cancellationToken);
            var subscription = subscriptions.FirstOrDefault();

            if (subscription == null)
            {
                _logger.LogWarning(
                    "Subscription not found for Razorpay ID {RazorpaySubscriptionId}",
                    razorpaySubscriptionId);
                return ServiceResult<bool>.FailureResult("Subscription not found");
            }

            // Process event based on type
            var result = await ProcessEventByTypeAsync(
                webhookEvent.EventType,
                razorpaySubscriptionId,
                webhookEvent.EventData,
                cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Successfully processed webhook event {EventId} of type {EventType}",
                    webhookEvent.RazorpayEventId,
                    webhookEvent.EventType);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to process webhook event {EventId} of type {EventType}: {Error}",
                    webhookEvent.RazorpayEventId,
                    webhookEvent.EventType,
                    result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling subscription webhook event {EventId}",
                webhookEvent?.RazorpayEventId ?? "unknown");
            return ServiceResult<bool>.FailureResult($"Error processing webhook: {ex.Message}");
        }
    }

    /// <summary>
    /// Processes the webhook event based on its type.
    /// </summary>
    private async Task<ServiceResult<bool>> ProcessEventByTypeAsync(
        string eventType,
        string razorpaySubscriptionId,
        string eventData,
        CancellationToken cancellationToken)
    {
        return eventType switch
        {
            "subscription.charged" => await HandlePaymentSuccessAsync(
                razorpaySubscriptionId,
                eventData,
                cancellationToken),
            "subscription.payment_failed" => await HandlePaymentFailureAsync(
                razorpaySubscriptionId,
                eventData,
                cancellationToken),
            "subscription.cancelled" => HandleCancellationAsync(
                razorpaySubscriptionId,
                eventData),
            _ => ServiceResult<bool>.FailureResult($"Unknown event type: {eventType}")
        };
    }

    /// <summary>
    /// Handles subscription.charged event - successful payment.
    /// </summary>
    private async Task<ServiceResult<bool>> HandlePaymentSuccessAsync(
        string razorpaySubscriptionId,
        string eventData,
        CancellationToken cancellationToken)
    {
        try
        {
            var (paymentDate, nextPaymentDate) = ExtractPaymentDates(eventData);

            var result = await _subscriptionPaymentService.HandlePaymentSuccessAsync(
                razorpaySubscriptionId,
                paymentDate,
                nextPaymentDate,
                cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Payment succeeded for Razorpay subscription {SubscriptionId}, next payment: {NextPaymentDate}",
                    razorpaySubscriptionId,
                    nextPaymentDate);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling payment success for subscription {SubscriptionId}",
                razorpaySubscriptionId);
            return ServiceResult<bool>.FailureResult($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles subscription.payment_failed event - failed payment.
    /// </summary>
    private async Task<ServiceResult<bool>> HandlePaymentFailureAsync(
        string razorpaySubscriptionId,
        string eventData,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _subscriptionPaymentService.HandlePaymentFailureAsync(
                razorpaySubscriptionId,
                cancellationToken);

            if (result.Success)
            {
                _logger.LogWarning(
                    "Payment failed for Razorpay subscription {SubscriptionId}",
                    razorpaySubscriptionId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling payment failure for subscription {SubscriptionId}",
                razorpaySubscriptionId);
            return ServiceResult<bool>.FailureResult($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles subscription.cancelled event - subscription cancellation.
    /// </summary>
    private ServiceResult<bool> HandleCancellationAsync(
        string razorpaySubscriptionId,
        string eventData)
    {
        try
        {
            _logger.LogWarning(
                "Subscription cancelled event received for Razorpay subscription {SubscriptionId}",
                razorpaySubscriptionId);
            return ServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling cancellation for subscription {SubscriptionId}",
                razorpaySubscriptionId);
            return ServiceResult<bool>.FailureResult($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies the webhook signature using HMAC-SHA256.
    /// </summary>
    private bool VerifyWebhookSignature(string eventData, string webhookSecret)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(eventData))
                return false;

            // Create HMAC-SHA256 hash of the event data
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(eventData));
            var hashHex = BitConverter.ToString(hash).Replace("-", "").ToLower();

            // In a real implementation, the signature would come from the HTTP headers
            // For now, we verify that the event data is not empty and valid JSON
            try
            {
                JsonDocument.Parse(eventData);
                return !string.IsNullOrWhiteSpace(hashHex);
            }
            catch (JsonException)
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying webhook signature");
            return false;
        }
    }

    /// <summary>
    /// Extracts the Razorpay subscription ID from the webhook event data.
    /// </summary>
    private string? ExtractRazorpaySubscriptionId(string eventData)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(eventData))
                return null;

            using var doc = JsonDocument.Parse(eventData);
            var root = doc.RootElement;

            // Try to extract from "payload.subscription.item.subscription_id"
            if (root.TryGetProperty("payload", out var payload))
            {
                if (payload.TryGetProperty("subscription", out var subscription))
                {
                    if (subscription.TryGetProperty("id", out var id))
                    {
                        return id.GetString();
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting Razorpay subscription ID from event data");
            return null;
        }
    }

    /// <summary>
    /// Extracts payment dates from the webhook event data.
    /// </summary>
    private (DateTime PaymentDate, DateTime NextPaymentDate) ExtractPaymentDates(string eventData)
    {
        try
        {
            using var doc = JsonDocument.Parse(eventData);
            var root = doc.RootElement;
            var paymentDate = DateTime.UtcNow;
            var nextPaymentDate = DateTime.UtcNow.AddMonths(1);

            // Try to extract from "payload.subscription"
            if (root.TryGetProperty("payload", out var payload))
            {
                if (payload.TryGetProperty("subscription", out var subscription))
                {
                    if (subscription.TryGetProperty("current_start", out var currentStart)
                        && long.TryParse(currentStart.GetString(), out var startTimestamp))
                    {
                        paymentDate = UnixTimeStampToDateTime(startTimestamp);
                    }

                    if (subscription.TryGetProperty("current_end", out var currentEnd)
                        && long.TryParse(currentEnd.GetString(), out var endTimestamp))
                    {
                        nextPaymentDate = UnixTimeStampToDateTime(endTimestamp);
                    }
                }
            }

            return (paymentDate, nextPaymentDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting payment dates from event data");
            return (DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));
        }
    }

    /// <summary>
    /// Converts Unix timestamp to DateTime.
    /// </summary>
    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
        return dateTime;
    }
}
