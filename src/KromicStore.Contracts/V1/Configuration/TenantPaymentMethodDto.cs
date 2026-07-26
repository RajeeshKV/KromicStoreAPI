namespace KromicStore.Contracts.V1.Configuration;

/// <summary>
/// Response DTO for tenant payment configuration.
/// Contains payment method details with masked secrets for security.
/// </summary>
public class TenantPaymentMethodDto
{
    /// <summary>
    /// Gets or sets the unique identifier for this configuration.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID that owns this configuration.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the payment provider name.
    /// Currently supports: Razorpay
    /// </summary>
    public string PaymentProvider { get; set; } = "Razorpay";

    /// <summary>
    /// Gets or sets the Razorpay Key ID (masked for security).
    /// Shows only the last 4 characters: "rzp_*****1234"
    /// </summary>
    public string RazorpayKeyIdMasked { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this configuration is active.
    /// Inactive configurations cannot be used for payment processing.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this configuration was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this configuration was last validated.
    /// Helps track if credentials are still valid.
    /// </summary>
    public DateTime? LastValidatedAt { get; set; }
}
