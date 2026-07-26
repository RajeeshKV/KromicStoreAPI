namespace KromicStore.Contracts.V1.Payments;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request to initiate a payment for an order via Razorpay.
/// </summary>
public class CreatePaymentRequest
{
    /// <summary>
    /// Gets or sets the order ID for which payment is being initiated.
    /// </summary>
    [Required(ErrorMessage = "Order ID is required")]
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the idempotency key to prevent duplicate payments.
    /// If not provided, it will be generated automatically.
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Gets or sets the payment method (e.g., "card", "upi", "netbanking").
    /// If not specified, customer can choose from available options.
    /// </summary>
    public string? PaymentMethod { get; set; }
}
