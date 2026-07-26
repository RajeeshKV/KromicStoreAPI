namespace KromicStore.Contracts.V1.Customers;

/// <summary>
/// Represents customer details in the response.
/// </summary>
public record CustomerDto(
    /// <summary>
    /// The unique identifier of the customer.
    /// </summary>
    Guid Id,
    
    /// <summary>
    /// The customer's first name.
    /// </summary>
    string FirstName,
    
    /// <summary>
    /// The customer's last name.
    /// </summary>
    string LastName,
    
    /// <summary>
    /// The customer's email address.
    /// </summary>
    string Email,
    
    /// <summary>
    /// The customer's phone number (optional).
    /// </summary>
    string? PhoneNumber,
    
    /// <summary>
    /// The customer's billing address (optional).
    /// </summary>
    AddressDto? BillingAddress,
    
    /// <summary>
    /// The customer's shipping address (optional).
    /// </summary>
    AddressDto? ShippingAddress,
    
    /// <summary>
    /// The total lifetime value of all customer purchases.
    /// </summary>
    decimal LifetimeValue,
    
    /// <summary>
    /// The total number of orders placed by the customer.
    /// </summary>
    int TotalOrdersCount,
    
    /// <summary>
    /// Whether the customer account is active.
    /// </summary>
    bool IsActive,
    
    /// <summary>
    /// The UTC timestamp when the customer was registered.
    /// </summary>
    DateTime RegisteredAt);
