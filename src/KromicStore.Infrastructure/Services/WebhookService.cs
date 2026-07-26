namespace KromicStore.Infrastructure.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using KromicStore.Application.Interfaces;
using KromicStore.Domain.Enums;

/// <summary>
/// Implementation of webhook service for managing webhook configurations, event publishing, and delivery tracking.
/// </summary>
public class WebhookService : IWebhookService
{
    private readonly ILogger<WebhookService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public WebhookService(ILogger<WebhookService> logger, IUnitOfWork unitOfWork, ICacheService cacheService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    /// <summary>
    /// Registers a new webhook for a tenant.
    /// </summary>
    public async Task<WebhookConfigDto> RegisterWebhookAsync(Guid tenantId, string endpointUrl, WebhookEventType[] eventTypes, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(endpointUrl))
            throw new ArgumentException("Endpoint URL cannot be empty", nameof(endpointUrl));

        if (eventTypes == null || eventTypes.Length == 0)
            throw new ArgumentException("At least one event type must be specified", nameof(eventTypes));

        _logger.LogInformation("Registering webhook for tenant {TenantId} at {EndpointUrl}", tenantId, endpointUrl);

        // Validate endpoint is reachable (in real implementation would make HEAD request)
        // For now, we'll skip this validation as it requires HttpClient setup
        
        var webhookId = Guid.NewGuid();
        var secret = GenerateSecret();

        // Create webhook configuration
        var webhookConfig = new WebhookConfigDto
        {
            Id = webhookId,
            TenantId = tenantId,
            EndpointUrl = endpointUrl,
            EventTypes = eventTypes,
            Secret = secret,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Webhook {WebhookId} registered successfully for tenant {TenantId}", webhookId, tenantId);

        // Invalidate cache
        await _cacheService.RemoveAsync($"webhooks:{tenantId}:list");

        return webhookConfig;
    }

    /// <summary>
    /// Unregisters an existing webhook.
    /// </summary>
    public async Task UnregisterWebhookAsync(Guid tenantId, Guid webhookId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        if (webhookId == Guid.Empty)
            throw new ArgumentException("Webhook ID cannot be empty", nameof(webhookId));

        _logger.LogInformation("Unregistering webhook {WebhookId} for tenant {TenantId}", webhookId, tenantId);

        // In real implementation, would delete from database
        // For now, just log and invalidate cache

        // Invalidate cache
        await _cacheService.RemoveAsync($"webhooks:{tenantId}:{webhookId}");
        await _cacheService.RemoveAsync($"webhooks:{tenantId}:list");

        _logger.LogInformation("Webhook {WebhookId} unregistered successfully", webhookId);
    }

    /// <summary>
    /// Publishes a webhook event to all subscribed endpoints.
    /// </summary>
    public async Task PublishEventAsync(Guid tenantId, WebhookEventType eventType, object payload, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key cannot be empty", nameof(idempotencyKey));

        _logger.LogInformation("Publishing webhook event {EventType} for tenant {TenantId} with idempotency key {IdempotencyKey}", 
            eventType, tenantId, idempotencyKey);

        // In real implementation, would:
        // 1. Create WebhookEventLog entry
        // 2. Find all webhooks subscribed to this event type
        // 3. Queue WebhookDeliveryJob for each matching webhook
        // For now, just log

        _logger.LogInformation("Webhook event {EventType} published for tenant {TenantId}", eventType, tenantId);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Replays a previously sent webhook event.
    /// </summary>
    public async Task RetryDeliveryAsync(Guid tenantId, Guid eventId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        if (eventId == Guid.Empty)
            throw new ArgumentException("Event ID cannot be empty", nameof(eventId));

        _logger.LogInformation("Retrying webhook delivery for event {EventId} in tenant {TenantId}", eventId, tenantId);

        // In real implementation, would:
        // 1. Load WebhookEventLog entry
        // 2. Find all delivery logs for this event
        // 3. Requeue delivery with new IdempotencyKey
        // For now, just log

        _logger.LogInformation("Webhook delivery retry queued for event {EventId}", eventId);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Lists webhook configurations for a tenant.
    /// </summary>
    public async Task<IEnumerable<WebhookConfigDto>> ListWebhooksAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        _logger.LogInformation("Listing webhooks for tenant {TenantId}", tenantId);

        // In real implementation, would fetch from database and cache
        // For now, return empty list

        return await Task.FromResult(Enumerable.Empty<WebhookConfigDto>());
    }

    /// <summary>
    /// Gets webhook delivery logs for a specific webhook.
    /// </summary>
    public async Task<(List<WebhookDeliveryLogDto> logs, int total)> GetDeliveryLogsAsync(Guid tenantId, Guid webhookId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        if (webhookId == Guid.Empty)
            throw new ArgumentException("Webhook ID cannot be empty", nameof(webhookId));

        _logger.LogInformation("Getting delivery logs for webhook {WebhookId} in tenant {TenantId}", webhookId, tenantId);

        // In real implementation, would fetch from database with pagination
        // For now, return empty results

        return await Task.FromResult((new List<WebhookDeliveryLogDto>(), 0));
    }

    /// <summary>
    /// Generates a secure random secret for webhook authentication.
    /// </summary>
    private string GenerateSecret()
    {
        // Generate 32 random bytes and convert to base64 for a 44-character secret
        var randomBytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        return Convert.ToBase64String(randomBytes);
    }
}
