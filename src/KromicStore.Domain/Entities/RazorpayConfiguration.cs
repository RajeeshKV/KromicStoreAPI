namespace KromicStore.Domain.Entities;

/// <summary>
/// Represents Razorpay payment gateway configuration for a tenant.
/// Stores API keys and settings for processing payments.
/// </summary>
public class RazorpayConfiguration : BaseEntity
{
    /// <summary>Gets the tenant ID this configuration belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the Razorpay Key ID (public key).</summary>
    public string KeyId { get; private set; } = string.Empty;

    /// <summary>Gets the Razorpay Key Secret (encrypted).</summary>
    public string KeySecret { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether this configuration is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets the environment (Test or Live).</summary>
    public string Environment { get; private set; } = "Test";

    /// <summary>Gets the webhook secret for Razorpay webhooks (optional).</summary>
    public string? WebhookSecret { get; private set; }

    /// <summary>Gets the description or notes for this configuration.</summary>
    public string? Description { get; private set; }

    /// <summary>Navigation property to the tenant.</summary>
    public Tenant? Tenant { get; private set; }

    /// <summary>
    /// Creates a new instance of RazorpayConfiguration.
    /// </summary>
    public static RazorpayConfiguration Create(Guid tenantId, string keyId, string keySecret, string environment = "Test")
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(keyId))
            throw new ArgumentException("Key ID is required.", nameof(keyId));
        if (string.IsNullOrWhiteSpace(keySecret))
            throw new ArgumentException("Key Secret is required.", nameof(keySecret));

        return new RazorpayConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            KeyId = keyId.Trim(),
            KeySecret = keySecret.Trim(),
            Environment = environment.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates the Razorpay configuration.
    /// </summary>
    public void UpdateConfig(string keyId, string keySecret, string environment, string? webhookSecret = null, string? description = null)
    {
        if (!string.IsNullOrWhiteSpace(keyId))
            KeyId = keyId.Trim();

        if (!string.IsNullOrWhiteSpace(keySecret))
            KeySecret = keySecret.Trim();

        if (!string.IsNullOrWhiteSpace(environment))
            Environment = environment.Trim();

        WebhookSecret = webhookSecret?.Trim();
        Description = description?.Trim();

        UpdateTimestamp();
    }

    /// <summary>
    /// Activates this configuration.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdateTimestamp();
    }

    /// <summary>
    /// Deactivates this configuration.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }
}
