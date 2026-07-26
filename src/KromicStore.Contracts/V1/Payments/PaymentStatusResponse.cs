namespace KromicStore.Contracts.V1.Payments;

/// <summary>
/// Response containing payment status and details.
/// </summary>
public class PaymentStatusResponse
{
    /// <summary>
    /// Gets or sets the payment ID.
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Gets or sets the Razorpay order ID.
    /// </summary>
    public string? RazorpayOrderId { get; set; }

    /// <summary>
    /// Gets or sets the Razorpay payment ID (once payment is completed).
    /// </summary>
    public string? RazorpayPaymentId { get; set; }

    /// <summary>
    /// Gets or sets the payment status (Pending, Processing, Completed, Failed, Refunded).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the payment amount in base currency.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency code (e.g., "INR").
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the payment method used (e.g., "card", "upi", "netbanking").
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Gets or sets when the payment was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the payment was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the payment was completed (if applicable).
    /// </summary>
    public DateTime? PaidAt { get; set; }
}
