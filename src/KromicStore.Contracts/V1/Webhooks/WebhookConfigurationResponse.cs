namespace KromicStore.Contracts.V1.Webhooks;

using KromicStore.Domain.Enums;
using System;
using System.Collections.Generic;

/// <summary>
/// Response DTO for webhook configuration.
/// </summary>
public class WebhookConfigurationResponse
{
    /// <summary>
    /// Gets or sets the webhook configuration ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the endpoint URL.
    /// </summary>
    public string EndpointUrl { get; set; } = null!;

    /// <summary>
    /// Gets or sets the subscribed event types.
    /// </summary>
    public IEnumerable<WebhookEventType> EventTypes { get; set; } = new List<WebhookEventType>();

    /// <summary>
    /// Gets or sets the webhook secret (only returned on creation).
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this webhook is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last delivery attempt.
    /// </summary>
    public DateTime? LastDeliveryAt { get; set; }
}
