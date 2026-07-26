namespace KromicStore.Domain.Enums;

/// <summary>
/// Enumeration of payment transaction types.
/// </summary>
public enum PaymentTransactionType
{
    /// <summary>Debit transaction (payment).</summary>
    Debit = 1,

    /// <summary>Credit transaction (reversal).</summary>
    Credit = 2,

    /// <summary>Refund transaction.</summary>
    Refund = 3
}
