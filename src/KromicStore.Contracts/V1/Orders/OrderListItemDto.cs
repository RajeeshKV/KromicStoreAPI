namespace KromicStore.Contracts.V1.Orders;

/// <summary>
/// Represents an order item in a list response.
/// Contains essential order information for list views.
/// </summary>
public record OrderListItemDto(
    /// <summary>
    /// The unique identifier of the order.
    /// </summary>
    Guid Id,
    
    /// <summary>
    /// The human-readable order number.
    /// </summary>
    string OrderNumber,
    
    /// <summary>
    /// The current status of the order.
    /// </summary>
    string Status,
    
    /// <summary>
    /// The total amount for the order.
    /// </summary>
    decimal Total,
    
    /// <summary>
    /// The UTC timestamp when the order was created.
    /// </summary>
    DateTime CreatedAt);
