#nullable disable

namespace KromicStore.Application.Interfaces;

using KromicStore.Domain.Enums;

/// <summary>
/// Interface for webhook service providing webhook registration, event publishing, and delivery management.
/// </summary>
public interface IWebhookService
{
    /// <summary>
    /// Registers a new webhook for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID owning the webhook</param>
    /// <param name="endpointUrl">The endpoint URL to deliver webhook events to</param>
    /// <param name="eventTypes">The event types to subscribe to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The registered webhook configuration including the generated secret</returns>
    Task<WebhookConfigDto> RegisterWebhookAsync(Guid tenantId, string endpointUrl, WebhookEventType[] eventTypes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters an existing webhook.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="webhookId">The webhook configuration ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UnregisterWebhookAsync(Guid tenantId, Guid webhookId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a webhook event to all subscribed endpoints.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="eventType">The event type</param>
    /// <param name="payload">The event payload</param>
    /// <param name="idempotencyKey">Idempotency key for deduplication</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishEventAsync(Guid tenantId, WebhookEventType eventType, object payload, string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replays a previously sent webhook event.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="eventId">The webhook event ID to replay</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RetryDeliveryAsync(Guid tenantId, Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists webhook configurations for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of webhook configurations</returns>
    Task<IEnumerable<WebhookConfigDto>> ListWebhooksAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets webhook delivery logs for a specific webhook.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="webhookId">The webhook configuration ID</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Delivery logs with pagination</returns>
    Task<(List<WebhookDeliveryLogDto> logs, int total)> GetDeliveryLogsAsync(Guid tenantId, Guid webhookId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for webhook configuration.
/// </summary>
public class WebhookConfigDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string EndpointUrl { get; set; }
    public WebhookEventType[] EventTypes { get; set; }
    public string Secret { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for webhook delivery log.
/// </summary>
public class WebhookDeliveryLogDto
{
    public Guid Id { get; set; }
    public Guid WebhookConfigId { get; set; }
    public Guid EventId { get; set; }
    public WebhookEventType EventType { get; set; }
    public int HttpStatusCode { get; set; }
    public string Response { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
}
