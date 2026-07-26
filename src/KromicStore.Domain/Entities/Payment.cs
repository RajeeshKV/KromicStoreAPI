namespace KromicStore.Domain.Entities;

using Enums;
using ValueObjects;

/// <summary>
/// Represents a payment for an order.
/// </summary>
public class Payment : BaseEntity
{
    /// <summary>Gets the tenant ID this payment belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the order ID this payment is for.</summary>
    public Guid OrderId { get; private set; }

    /// <summary>Gets the payment amount.</summary>
    public Money Amount { get; private set; }

    /// <summary>Gets the payment status.</summary>
    public PaymentStatus Status { get; private set; }

    /// <summary>Gets the external payment ID from Razorpay.</summary>
    public string? ExternalPaymentId { get; private set; }

    /// <summary>Gets the payment method used (card, upi, netbanking, etc).</summary>
    public string? PaymentMethod { get; private set; }

    /// <summary>Gets the timestamp when payment was completed.</summary>
    public DateTime? PaidAt { get; private set; }

    /// <summary>Gets the failure reason if payment failed.</summary>
    public string? FailureReason { get; private set; }

    private List<PaymentTransaction> _transactions = new();

    /// <summary>Gets the payment transactions history.</summary>
    public IReadOnlyList<PaymentTransaction> Transactions => _transactions.AsReadOnly();

    /// <summary>
    /// Creates a new instance of Payment.
    /// </summary>
    public static Payment Create(
        Guid tenantId,
        Guid orderId,
        Money amount)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        if (amount.Amount <= 0)
            throw new ArgumentException("Payment amount must be greater than zero.", nameof(amount));

        return new Payment
        {
            TenantId = tenantId,
            OrderId = orderId,
            Amount = amount,
            Status = PaymentStatus.Pending
        };
    }

    /// <summary>
    /// Marks the payment as processed with external payment ID.
    /// </summary>
    public void MarkAsProcessed(string externalPaymentId, string? paymentMethod = null)
    {
        if (string.IsNullOrWhiteSpace(externalPaymentId))
            throw new ArgumentException("External payment ID is required.", nameof(externalPaymentId));

        if (Status != PaymentStatus.Pending && Status != PaymentStatus.Initiated)
            throw new InvalidOperationException($"Cannot mark payment as processed from status {Status}.");

        ExternalPaymentId = externalPaymentId;
        PaymentMethod = paymentMethod;
        Status = PaymentStatus.Completed;
        PaidAt = DateTime.UtcNow;

        // Record the payment transaction
        var transaction = PaymentTransaction.Create(
            Id,
            Amount,
            Domain.Enums.PaymentTransactionType.Debit,
            externalPaymentId);
        _transactions.Add(transaction);
    }

    /// <summary>
    /// Marks the payment as failed with a reason.
    /// </summary>
    public void MarkAsFailed(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Failure reason is required.", nameof(reason));

        if (Status == PaymentStatus.Failed)
            throw new InvalidOperationException("Payment is already marked as failed.");

        Status = PaymentStatus.Failed;
        FailureReason = reason;
    }

    /// <summary>
    /// Records a refund transaction.
    /// </summary>
    public void RecordRefund(Money refundAmount, string reason = "")
    {
        if (refundAmount.Amount <= 0)
            throw new ArgumentException("Refund amount must be positive.", nameof(refundAmount));

        if (Status != PaymentStatus.Completed)
            throw new InvalidOperationException("Only completed payments can be refunded.");

        var refundTransaction = PaymentTransaction.Create(
            Id,
            refundAmount,
            Domain.Enums.PaymentTransactionType.Refund,
            $"Refund-{Guid.NewGuid():N}");
        refundTransaction.SetNotes(reason);

        _transactions.Add(refundTransaction);

        // Update status if fully refunded
        var totalRefunded = _transactions
            .Where(t => t.TransactionType == Domain.Enums.PaymentTransactionType.Refund)
            .Sum(t => t.Amount.Amount);

        if (totalRefunded >= Amount.Amount)
        {
            Status = PaymentStatus.Refunded;
        }
    }

    /// <summary>
    /// Marks payment as initiated (for transient state).
    /// </summary>
    public void MarkAsInitiated()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Only pending payments can be marked as initiated.");

        Status = PaymentStatus.Initiated;
    }
}
