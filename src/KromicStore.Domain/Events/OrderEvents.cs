namespace KromicStore.Domain.Events;

/// <summary>
/// Domain event published when an order is created.
/// </summary>
public class OrderCreatedEvent : DomainEvent
{
    /// <summary>Gets the order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets the customer ID.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Gets the order total amount.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Initializes a new instance of OrderCreatedEvent.
    /// </summary>
    public OrderCreatedEvent(Guid tenantId, Guid orderId, string orderNumber, Guid customerId, decimal totalAmount)
    {
        TenantId = tenantId;
        EntityId = orderId;
        OrderNumber = orderNumber;
        CustomerId = customerId;
        TotalAmount = totalAmount;
    }
}

/// <summary>
/// Domain event published when order status changes.
/// </summary>
public class OrderStatusChangedEvent : DomainEvent
{
    /// <summary>Gets the order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets the customer ID.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Gets the new order status.</summary>
    public string NewStatus { get; set; } = string.Empty;

    /// <summary>Gets the previous order status.</summary>
    public string PreviousStatus { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of OrderStatusChangedEvent.
    /// </summary>
    public OrderStatusChangedEvent(Guid tenantId, Guid orderId, string orderNumber, Guid customerId, string newStatus, string previousStatus)
    {
        TenantId = tenantId;
        EntityId = orderId;
        OrderNumber = orderNumber;
        CustomerId = customerId;
        NewStatus = newStatus;
        PreviousStatus = previousStatus;
    }
}

/// <summary>
/// Domain event published when an order is confirmed.
/// </summary>
public class OrderConfirmedEvent : DomainEvent
{
    /// <summary>Gets the order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets the customer ID.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Initializes a new instance of OrderConfirmedEvent.
    /// </summary>
    public OrderConfirmedEvent(Guid tenantId, Guid orderId, string orderNumber, Guid customerId)
    {
        TenantId = tenantId;
        EntityId = orderId;
        OrderNumber = orderNumber;
        CustomerId = customerId;
    }
}

/// <summary>
/// Domain event published when payment is recorded for an order.
/// </summary>
public class OrderPaidEvent : DomainEvent
{
    /// <summary>Gets the order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets the customer ID.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Gets the payment method used.</summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>Gets the payment gateway reference.</summary>
    public string? PaymentGatewayReference { get; set; }

    /// <summary>
    /// Initializes a new instance of OrderPaidEvent.
    /// </summary>
    public OrderPaidEvent(Guid tenantId, Guid orderId, string orderNumber, Guid customerId, string paymentMethod, string? gatewayReference = null)
    {
        TenantId = tenantId;
        EntityId = orderId;
        OrderNumber = orderNumber;
        CustomerId = customerId;
        PaymentMethod = paymentMethod;
        PaymentGatewayReference = gatewayReference;
    }
}

/// <summary>
/// Domain event published when an order is shipped.
/// </summary>
public class OrderShippedEvent : DomainEvent
{
    /// <summary>Gets the order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets the customer ID.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Gets the tracking number.</summary>
    public string TrackingNumber { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of OrderShippedEvent.
    /// </summary>
    public OrderShippedEvent(Guid tenantId, Guid orderId, string orderNumber, Guid customerId, string trackingNumber)
    {
        TenantId = tenantId;
        EntityId = orderId;
        OrderNumber = orderNumber;
        CustomerId = customerId;
        TrackingNumber = trackingNumber;
    }
}

/// <summary>
/// Domain event published when an order is delivered.
/// </summary>
public class OrderDeliveredEvent : DomainEvent
{
    /// <summary>Gets the order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets the customer ID.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Initializes a new instance of OrderDeliveredEvent.
    /// </summary>
    public OrderDeliveredEvent(Guid tenantId, Guid orderId, string orderNumber, Guid customerId)
    {
        TenantId = tenantId;
        EntityId = orderId;
        OrderNumber = orderNumber;
        CustomerId = customerId;
    }
}

/// <summary>
/// Domain event published when an order is cancelled.
/// </summary>
public class OrderCancelledEvent : DomainEvent
{
    /// <summary>Gets the order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets the customer ID.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Gets the cancellation reason.</summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Initializes a new instance of OrderCancelledEvent.
    /// </summary>
    public OrderCancelledEvent(Guid tenantId, Guid orderId, string orderNumber, Guid customerId, string? reason = null)
    {
        TenantId = tenantId;
        EntityId = orderId;
        OrderNumber = orderNumber;
        CustomerId = customerId;
        CancellationReason = reason;
    }
}
