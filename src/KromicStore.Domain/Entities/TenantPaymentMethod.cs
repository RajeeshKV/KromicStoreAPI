namespace KromicStore.Domain.Entities;

/// <summary>
/// Represents payment method configuration for a tenant (e.g., Razorpay credentials).
/// Stores encrypted credentials for tenant's payment processor.
/// </summary>
public class TenantPaymentMethod : BaseEntity
{
    /// <summary>Gets the tenant ID this payment method belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the payment provider type (e.g., "razorpay").</summary>
    public string Provider { get; private set; } = "razorpay";

    /// <summary>Gets the encrypted API key (stores Razorpay KEY_ID or equivalent).</summary>
    public string EncryptedApiKey { get; private set; } = string.Empty;

    /// <summary>Gets the encrypted API secret (stores Razorpay KEY_SECRET or equivalent).</summary>
    public string EncryptedApiSecret { get; private set; } = string.Empty;

    /// <summary>Gets the encrypted webhook secret.</summary>
    public string EncryptedWebhookSecret { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether this payment method is enabled.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>Gets a value indicating whether test mode is enabled.</summary>
    public bool TestModeEnabled { get; private set; }

    /// <summary>Gets the date when credentials were last tested.</summary>
    public DateTime? LastTestedAt { get; private set; }

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private TenantPaymentMethod() { }

    /// <summary>
    /// Creates a new payment method configuration for a tenant.
    /// </summary>
    public static TenantPaymentMethod Create(
        Guid tenantId,
        string provider,
        string encryptedApiKey,
        string encryptedApiSecret,
        string encryptedWebhookSecret)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider is required", nameof(provider));
        if (string.IsNullOrWhiteSpace(encryptedApiKey))
            throw new ArgumentException("API key is required", nameof(encryptedApiKey));
        if (string.IsNullOrWhiteSpace(encryptedApiSecret))
            throw new ArgumentException("API secret is required", nameof(encryptedApiSecret));
        if (string.IsNullOrWhiteSpace(encryptedWebhookSecret))
            throw new ArgumentException("Webhook secret is required", nameof(encryptedWebhookSecret));

        return new TenantPaymentMethod
        {
            TenantId = tenantId,
            Provider = provider.ToLowerInvariant(),
            EncryptedApiKey = encryptedApiKey,
            EncryptedApiSecret = encryptedApiSecret,
            EncryptedWebhookSecret = encryptedWebhookSecret,
            IsEnabled = true,
            TestModeEnabled = false
        };
    }

    /// <summary>
    /// Updates the payment method credentials.
    /// </summary>
    public void UpdateCredentials(
        string encryptedApiKey,
        string encryptedApiSecret,
        string encryptedWebhookSecret)
    {
        if (string.IsNullOrWhiteSpace(encryptedApiKey))
            throw new ArgumentException("API key is required", nameof(encryptedApiKey));
        if (string.IsNullOrWhiteSpace(encryptedApiSecret))
            throw new ArgumentException("API secret is required", nameof(encryptedApiSecret));
        if (string.IsNullOrWhiteSpace(encryptedWebhookSecret))
            throw new ArgumentException("Webhook secret is required", nameof(encryptedWebhookSecret));

        EncryptedApiKey = encryptedApiKey;
        EncryptedApiSecret = encryptedApiSecret;
        EncryptedWebhookSecret = encryptedWebhookSecret;
        UpdatedAt = DateTime.UtcNow;
        LastTestedAt = null; // Reset test status when credentials change
    }

    /// <summary>
    /// Marks this payment method as tested successfully.
    /// </summary>
    public void MarkAsTested()
    {
        LastTestedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disables this payment method.
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Enables this payment method.
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
