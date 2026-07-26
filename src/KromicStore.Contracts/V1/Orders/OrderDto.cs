namespace KromicStore.Contracts.V1.Orders;

/// <summary>
/// Represents order details in the response.
/// </summary>
public record OrderDto(
    /// <summary>
    /// The unique identifier of the order.
    /// </summary>
    Guid Id,
    
    /// <summary>
    /// The human-readable order number (e.g., ORD-20240115-00001).
    /// </summary>
    string OrderNumber,
    
    /// <summary>
    /// The current status of the order (Pending, Confirmed, Paid, Shipped, Delivered, Cancelled).
    /// </summary>
    string Status,
    
    /// <summary>
    /// The subtotal amount before tax and shipping.
    /// </summary>
    decimal Subtotal,
    
    /// <summary>
    /// The tax amount calculated on the subtotal.
    /// </summary>
    decimal TaxAmount,
    
    /// <summary>
    /// The shipping cost for the order.
    /// </summary>
    decimal ShippingCost,
    
    /// <summary>
    /// The total amount including subtotal, tax, and shipping.
    /// </summary>
    decimal Total,
    
    /// <summary>
    /// The payment status for the order.
    /// </summary>
    string PaymentStatus,
    
    /// <summary>
    /// The payment method used (optional).
    /// </summary>
    string? PaymentMethod,
    
    /// <summary>
    /// The tracking number for shipped orders (optional).
    /// </summary>
    string? TrackingNumber,
    
    /// <summary>
    /// The shipping address for the order.
    /// </summary>
    AddressDto? ShippingAddress,
    
    /// <summary>
    /// The collection of items in the order.
    /// </summary>
    IReadOnlyList<OrderItemDto> Items,
    
    /// <summary>
    /// The UTC timestamp when the order was created.
    /// </summary>
    DateTime CreatedAt);
