namespace KromicStore.Application.Models;

using KromicStore.Domain.Enums;
using System;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a webhook event to be delivered to external systems.
/// Contains all event metadata and payload information.
/// </summary>
public class WebhookEvent
{
    /// <summary>
    /// Gets the unique event identifier.
    /// </summary>
    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets the type of webhook event.
    /// </summary>
    [JsonPropertyName("eventType")]
    public WebhookEventType EventType { get; set; }

    /// <summary>
    /// Gets the timestamp when the event occurred (ISO 8601 format).
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets the tenant ID associated with this event.
    /// </summary>
    [JsonPropertyName("tenantId")]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets the idempotency key for deduplication by webhook consumers.
    /// Allows safe retries without duplicate processing.
    /// </summary>
    [JsonPropertyName("idempotencyKey")]
    public string IdempotencyKey { get; set; } = null!;

    /// <summary>
    /// Gets the event-specific payload (serialized from event source data).
    /// Structure varies based on EventType.
    /// </summary>
    [JsonPropertyName("payload")]
    public object Payload { get; set; } = null!;

    /// <summary>
    /// Gets the API version of the event schema.
    /// Used for versioning and backward compatibility.
    /// </summary>
    [JsonPropertyName("apiVersion")]
    public int ApiVersion { get; set; } = 1;

    /// <summary>
    /// Initializes a new instance of the WebhookEvent class.
    /// </summary>
    public WebhookEvent()
    {
        EventId = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Factory method to create a new WebhookEvent.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="eventType">The event type.</param>
    /// <param name="payload">The event payload.</param>
    /// <param name="idempotencyKey">The idempotency key.</param>
    /// <returns>A new WebhookEvent instance.</returns>
    public static WebhookEvent Create(
        Guid tenantId,
        WebhookEventType eventType,
        object payload,
        string idempotencyKey)
    {
        return new WebhookEvent
        {
            EventId = Guid.NewGuid(),
            TenantId = tenantId,
            EventType = eventType,
            Payload = payload,
            IdempotencyKey = idempotencyKey,
            Timestamp = DateTime.UtcNow,
            ApiVersion = 1
        };
    }
}
