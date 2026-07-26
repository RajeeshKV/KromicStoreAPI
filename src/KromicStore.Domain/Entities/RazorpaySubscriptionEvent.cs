namespace KromicStore.Domain.Entities;

/// <summary>
/// Represents a webhook event received from Razorpay subscription API.
/// Used for audit trail and idempotent webhook processing.
/// </summary>
public class RazorpaySubscriptionEvent : BaseEntity
{
    /// <summary>Gets the subscription ID this event belongs to.</summary>
    public Guid SubscriptionId { get; private set; }

    /// <summary>Gets the Razorpay subscription ID.</summary>
    public string RazorpaySubscriptionId { get; private set; } = string.Empty;

    /// <summary>Gets the event type (e.g., "subscription.charged", "subscription.failed").</summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>Gets the unique Razorpay event ID for idempotency.</summary>
    public string RazorpayEventId { get; private set; } = string.Empty;

    /// <summary>Gets the full webhook payload as JSON.</summary>
    public string EventData { get; private set; } = string.Empty;

    /// <summary>Gets the date when the event was processed.</summary>
    public DateTime ProcessedAt { get; private set; }

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private RazorpaySubscriptionEvent() { }

    /// <summary>
    /// Creates a new subscription event from webhook payload.
    /// </summary>
    public static RazorpaySubscriptionEvent Create(
        Guid subscriptionId,
        string razorpaySubscriptionId,
        string eventType,
        string razorpayEventId,
        string eventDataJson)
    {
        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(razorpaySubscriptionId))
            throw new ArgumentException("Razorpay subscription ID is required", nameof(razorpaySubscriptionId));
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type is required", nameof(eventType));
        if (string.IsNullOrWhiteSpace(razorpayEventId))
            throw new ArgumentException("Razorpay event ID is required", nameof(razorpayEventId));

        return new RazorpaySubscriptionEvent
        {
            SubscriptionId = subscriptionId,
            RazorpaySubscriptionId = razorpaySubscriptionId,
            EventType = eventType,
            RazorpayEventId = razorpayEventId,
            EventData = eventDataJson ?? "{}",
            ProcessedAt = DateTime.UtcNow
        };
    }
}
