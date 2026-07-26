namespace KromicStore.Contracts.V1.Customers;

/// <summary>
/// Represents a customer item in a list response.
/// Contains essential customer information for list views.
/// </summary>
public record CustomerListItemDto(
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
    /// The total lifetime value of all customer purchases.
    /// </summary>
    decimal LifetimeValue,
    
    /// <summary>
    /// The total number of orders placed by the customer.
    /// </summary>
    int TotalOrdersCount,
    
    /// <summary>
    /// The UTC timestamp when the customer was registered.
    /// </summary>
    DateTime RegisteredAt);
