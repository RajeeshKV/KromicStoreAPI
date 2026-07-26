namespace KromicStore.Domain.ValueObjects;

/// <summary>
/// Represents a physical address.
/// </summary>
public record Address
{
    /// <summary>Gets the street address.</summary>
    public string Street { get; init; }

    /// <summary>Gets the city.</summary>
    public string City { get; init; }

    /// <summary>Gets the state or province.</summary>
    public string State { get; init; }

    /// <summary>Gets the postal code.</summary>
    public string PostalCode { get; init; }

    /// <summary>Gets the country.</summary>
    public string Country { get; init; }

    /// <summary>
    /// Creates a new instance of Address.
    /// </summary>
    public Address(string street, string city, string state, string postalCode, string country)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street address is required.", nameof(street));
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required.", nameof(city));
        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State is required.", nameof(state));
        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException("Postal code is required.", nameof(postalCode));
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country is required.", nameof(country));

        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }
}
