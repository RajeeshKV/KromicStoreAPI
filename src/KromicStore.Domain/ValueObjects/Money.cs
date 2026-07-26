namespace KromicStore.Domain.ValueObjects;

/// <summary>
/// Represents a monetary value with currency.
/// </summary>
public record Money
{
    /// <summary>
    /// Gets the amount.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Gets the currency code (ISO 4217).
    /// </summary>
    public string Currency { get; init; } = "INR";

    /// <summary>
    /// Creates a new instance of Money.
    /// </summary>
    /// <param name="amount">The monetary amount.</param>
    /// <param name="currency">The currency code.</param>
    /// <exception cref="ArgumentException">Thrown when amount is negative or currency is empty.</exception>
    public Money(decimal amount, string currency = "INR")
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency code is required.", nameof(currency));

        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Implicit conversion from decimal to Money (assumes INR currency).
    /// </summary>
    public static implicit operator Money(decimal amount) => new(amount);

    /// <summary>
    /// Implicit conversion from Money to decimal.
    /// </summary>
    public static implicit operator decimal(Money money) => money.Amount;
}
