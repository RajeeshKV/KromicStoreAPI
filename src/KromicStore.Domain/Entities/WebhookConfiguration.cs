namespace KromicStore.Domain.Entities;

using KromicStore.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

/// <summary>
/// Represents a webhook configuration for external system integration.
/// Stores endpoint URLs, subscribed event types, authentication credentials, and active status.
/// </summary>
public class WebhookConfiguration : BaseEntity
{
    /// <summary>
    /// Gets the tenant ID this webhook belongs to.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the endpoint URL where webhook events will be delivered.
    /// </summary>
    public string EndpointUrl { get; private set; } = null!;

    /// <summary>
    /// Gets the collection of event types this webhook is subscribed to.
    /// </summary>
    public ICollection<WebhookEventType> EventTypes { get; private set; } = new List<WebhookEventType>();

    /// <summary>
    /// Gets the secret key used for HMAC-SHA256 signature generation.
    /// Never returned in API responses.
    /// </summary>
    public string Secret { get; private set; } = null!;

    /// <summary>
    /// Gets a value indicating whether this webhook is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the optional custom authentication header value (e.g., "Bearer token").
    /// </summary>
    public string? AuthenticationHeader { get; private set; }

    /// <summary>
    /// Gets the description or name of this webhook.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the timestamp of the last delivery attempt.
    /// </summary>
    public DateTime? LastDeliveryAt { get; private set; }

    /// <summary>
    /// Initializes a new instance of the WebhookConfiguration class.
    /// </summary>
    private WebhookConfiguration()
    {
    }

    /// <summary>
    /// Factory method to create a new webhook configuration.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="endpointUrl">The endpoint URL.</param>
    /// <param name="eventTypes">The event types to subscribe to.</param>
    /// <param name="authenticationHeader">Optional custom authentication header.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>A new WebhookConfiguration instance with generated secret.</returns>
    public static WebhookConfiguration Create(
        Guid tenantId,
        string endpointUrl,
        IEnumerable<WebhookEventType> eventTypes,
        string? authenticationHeader = null,
        string? description = null)
    {
        var config = new WebhookConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EndpointUrl = endpointUrl,
            EventTypes = new List<WebhookEventType>(eventTypes),
            Secret = GenerateSecret(),
            IsActive = true,
            AuthenticationHeader = authenticationHeader,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return config;
    }

    /// <summary>
    /// Updates the endpoint URL.
    /// </summary>
    /// <param name="endpointUrl">The new endpoint URL.</param>
    public void UpdateEndpoint(string endpointUrl)
    {
        EndpointUrl = endpointUrl;
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the subscribed event types.
    /// </summary>
    /// <param name="eventTypes">The new event types.</param>
    public void UpdateEventTypes(IEnumerable<WebhookEventType> eventTypes)
    {
        EventTypes = new List<WebhookEventType>(eventTypes);
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the authentication header.
    /// </summary>
    /// <param name="authenticationHeader">The new authentication header or null.</param>
    public void UpdateAuthenticationHeader(string? authenticationHeader)
    {
        AuthenticationHeader = authenticationHeader;
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the active status.
    /// </summary>
    /// <param name="isActive">The new active status.</param>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdateTimestamp();
    }

    /// <summary>
    /// Rotates the webhook secret.
    /// </summary>
    public void RotateSecret()
    {
        Secret = GenerateSecret();
        UpdateTimestamp();
    }

    /// <summary>
    /// Records the last delivery attempt timestamp.
    /// </summary>
    public void RecordDeliveryAttempt()
    {
        LastDeliveryAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Generates a secure random secret for webhook signature generation.
    /// </summary>
    /// <returns>A Base64-encoded 64-byte secret.</returns>
    private static string GenerateSecret()
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            byte[] secret = new byte[64];
            rng.GetBytes(secret);
            return Convert.ToBase64String(secret);
        }
    }
}
