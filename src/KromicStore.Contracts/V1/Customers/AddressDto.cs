namespace KromicStore.Contracts.V1.Customers;

/// <summary>
/// Represents an address in a response (read-only).
/// Used in customer details and profiles.
/// </summary>
public record AddressDto(
    /// <summary>
    /// The street address line.
    /// </summary>
    string Street,
    
    /// <summary>
    /// The city name.
    /// </summary>
    string City,
    
    /// <summary>
    /// The state or province.
    /// </summary>
    string State,
    
    /// <summary>
    /// The postal/zip code.
    /// </summary>
    string PostalCode,
    
    /// <summary>
    /// The country name.
    /// </summary>
    string Country);
