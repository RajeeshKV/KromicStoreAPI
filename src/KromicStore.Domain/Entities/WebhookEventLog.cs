namespace KromicStore.Domain.Entities;

using KromicStore.Domain.Enums;
using System;

/// <summary>
/// Represents a logged webhook event for audit and replay purposes.
/// Stores event data, timestamp, and idempotency information.
/// </summary>
public class WebhookEventLog : BaseEntity
{
    /// <summary>
    /// Gets the tenant ID this event belongs to.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the unique event identifier.
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Gets the type of webhook event.
    /// </summary>
    public WebhookEventType EventType { get; private set; }

    /// <summary>
    /// Gets the JSON-serialized event payload.
    /// </summary>
    public string Payload { get; private set; } = null!;

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public DateTime OccurredAt { get; private set; }

    /// <summary>
    /// Gets the idempotency key for deduplication.
    /// Allows webhook consumers to safely retry without duplication.
    /// </summary>
    public string IdempotencyKey { get; private set; } = null!;

    /// <summary>
    /// Gets the API version of the event schema.
    /// </summary>
    public int ApiVersion { get; private set; } = 1;

    /// <summary>
    /// Initializes a new instance of the WebhookEventLog class.
    /// </summary>
    private WebhookEventLog()
    {
    }

    /// <summary>
    /// Factory method to create a new webhook event log entry.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="eventType">The event type.</param>
    /// <param name="payload">The JSON-serialized event payload.</param>
    /// <param name="idempotencyKey">The idempotency key.</param>
    /// <returns>A new WebhookEventLog instance.</returns>
    public static WebhookEventLog Create(
        Guid tenantId,
        WebhookEventType eventType,
        string payload,
        string idempotencyKey)
    {
        var eventLog = new WebhookEventLog
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            TenantId = tenantId,
            EventType = eventType,
            Payload = payload,
            IdempotencyKey = idempotencyKey,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return eventLog;
    }
}
