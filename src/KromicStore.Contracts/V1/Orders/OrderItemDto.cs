namespace KromicStore.Contracts.V1.Orders;

/// <summary>
/// Represents an individual item in an order.
/// Includes product information and pricing at the time of order.
/// </summary>
public record OrderItemDto(
    /// <summary>
    /// The unique identifier of the product.
    /// </summary>
    Guid ProductId,
    
    /// <summary>
    /// The name of the product as it was at the time of order.
    /// </summary>
    string ProductName,
    
    /// <summary>
    /// The quantity ordered.
    /// </summary>
    int Quantity,
    
    /// <summary>
    /// The unit price at the time the order was placed.
    /// </summary>
    decimal UnitPrice,
    
    /// <summary>
    /// The total price for this item (Quantity × UnitPrice).
    /// </summary>
    decimal TotalPrice);
