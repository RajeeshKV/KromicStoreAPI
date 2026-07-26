using KromicStore.Domain.ValueObjects;

namespace KromicStore.Domain.Entities;

/// <summary>
/// Represents a payment transaction for an order.
/// Links orders to Razorpay payment records.
/// </summary>
public class OrderPayment : BaseEntity
{
    /// <summary>Gets the order ID.</summary>
    public Guid OrderId { get; private set; }

    /// <summary>Gets the tenant ID (for data isolation).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the Razorpay order ID.</summary>
    public string RazorpayOrderId { get; private set; } = string.Empty;

    /// <summary>Gets the Razorpay payment ID (null until payment is made).</summary>
    public string? RazorpayPaymentId { get; private set; }

    /// <summary>Gets the payment amount.</summary>
    public Money Amount { get; private set; } = new Money(0);

    /// <summary>Gets the current payment status.</summary>
    public string Status { get; private set; } = "Initiated";

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private OrderPayment() { }

    /// <summary>
    /// Creates a new payment record for an order.
    /// </summary>
    public static OrderPayment Create(
        Guid orderId,
        Guid tenantId,
        string razorpayOrderId,
        Money amount)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID is required", nameof(orderId));
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(razorpayOrderId))
            throw new ArgumentException("Razorpay order ID is required", nameof(razorpayOrderId));
        if (amount.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        return new OrderPayment
        {
            OrderId = orderId,
            TenantId = tenantId,
            RazorpayOrderId = razorpayOrderId,
            Amount = amount,
            Status = "Initiated"
        };
    }

    /// <summary>
    /// Marks payment as authorized (Razorpay has verified it).
    /// </summary>
    public void AuthorizePayment(string razorpayPaymentId)
    {
        if (string.IsNullOrWhiteSpace(razorpayPaymentId))
            throw new ArgumentException("Payment ID is required", nameof(razorpayPaymentId));

        RazorpayPaymentId = razorpayPaymentId;
        Status = "Authorized";
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks payment as captured (money received).
    /// </summary>
    public void CapturePayment()
    {
        Status = "Captured";
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks payment as failed.
    /// </summary>
    public void MarkAsFailed()
    {
        Status = "Failed";
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks payment as refunded.
    /// </summary>
    public void MarkAsRefunded()
    {
        Status = "Refunded";
        UpdatedAt = DateTime.UtcNow;
    }
}
