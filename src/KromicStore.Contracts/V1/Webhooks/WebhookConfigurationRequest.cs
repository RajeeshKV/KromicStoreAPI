namespace KromicStore.Contracts.V1.Webhooks;

using KromicStore.Domain.Enums;
using System.Collections.Generic;

/// <summary>
/// Request DTO for creating or updating a webhook configuration.
/// </summary>
public class WebhookConfigurationRequest
{
    /// <summary>
    /// Gets or sets the endpoint URL where webhook events will be delivered.
    /// </summary>
    public string EndpointUrl { get; set; } = null!;

    /// <summary>
    /// Gets or sets the event types to subscribe to.
    /// </summary>
    public IEnumerable<WebhookEventType> EventTypes { get; set; } = new List<WebhookEventType>();

    /// <summary>
    /// Gets or sets the optional custom authentication header value.
    /// </summary>
    public string? AuthenticationHeader { get; set; }

    /// <summary>
    /// Gets or sets the optional description or name of this webhook.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this webhook is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
