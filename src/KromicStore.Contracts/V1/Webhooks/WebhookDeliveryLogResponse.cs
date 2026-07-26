namespace KromicStore.Contracts.V1.Webhooks;

using KromicStore.Domain.Enums;
using System;

/// <summary>
/// Response DTO for webhook delivery log.
/// </summary>
public class WebhookDeliveryLogResponse
{
    /// <summary>
    /// Gets or sets the delivery log ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the webhook configuration ID.
    /// </summary>
    public Guid WebhookConfigurationId { get; set; }

    /// <summary>
    /// Gets or sets the webhook event log ID.
    /// </summary>
    public Guid WebhookEventLogId { get; set; }

    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public WebhookEventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code received.
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// Gets or sets the response body (truncated to 1000 characters).
    /// </summary>
    public string? Response { get; set; }

    /// <summary>
    /// Gets or sets the number of delivery attempts.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when delivery was completed or failed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the next scheduled retry time.
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether delivery succeeded.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets an optional error message for diagnostic purposes.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
