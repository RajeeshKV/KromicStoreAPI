namespace KromicStore.Domain.Entities;

using Enums;
using ValueObjects;

/// <summary>
/// Represents a payment transaction record (immutable).
/// </summary>
public class PaymentTransaction : BaseEntity
{
    /// <summary>Gets the payment ID this transaction belongs to.</summary>
    public Guid PaymentId { get; private set; }

    /// <summary>Gets the transaction amount.</summary>
    public Money Amount { get; private set; }

    /// <summary>Gets the transaction type (Debit, Credit, Refund).</summary>
    public PaymentTransactionType TransactionType { get; private set; }

    /// <summary>Gets the transaction status.</summary>
    public PaymentStatus Status { get; private set; }

    /// <summary>Gets the external transaction ID from Razorpay.</summary>
    public string ExternalTransactionId { get; private set; } = string.Empty;

    /// <summary>Gets optional notes about the transaction.</summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Creates a new instance of PaymentTransaction (immutable after creation).
    /// </summary>
    public static PaymentTransaction Create(
        Guid paymentId,
        Money amount,
        PaymentTransactionType transactionType,
        string externalTransactionId)
    {
        if (paymentId == Guid.Empty)
            throw new ArgumentException("Payment ID is required.", nameof(paymentId));
        if (amount.Amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));
        if (string.IsNullOrWhiteSpace(externalTransactionId))
            throw new ArgumentException("External transaction ID is required.", nameof(externalTransactionId));

        return new PaymentTransaction
        {
            PaymentId = paymentId,
            Amount = amount,
            TransactionType = transactionType,
            Status = PaymentStatus.Completed,
            ExternalTransactionId = externalTransactionId
        };
    }

    /// <summary>
    /// Sets the notes for this transaction. Internal use only.
    /// </summary>
    internal void SetNotes(string? notes)
    {
        Notes = notes;
    }
}
