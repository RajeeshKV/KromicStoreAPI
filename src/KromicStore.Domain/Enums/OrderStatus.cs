namespace KromicStore.Domain.Enums;

/// <summary>
/// Enumeration of order statuses in the system.
/// </summary>
public enum OrderStatus
{
    /// <summary>Order has been created but not yet confirmed.</summary>
    Pending = 1,

    /// <summary>Order has been confirmed by the customer.</summary>
    Confirmed = 2,

    /// <summary>Payment has been received.</summary>
    Paid = 3,

    /// <summary>Order is being prepared for shipment.</summary>
    Processing = 4,

    /// <summary>Order has been shipped.</summary>
    Shipped = 5,

    /// <summary>Order has been delivered.</summary>
    Delivered = 6,

    /// <summary>Order has been cancelled.</summary>
    Cancelled = 7,

    /// <summary>Order has been refunded.</summary>
    Refunded = 8
}
