namespace KromicStore.Domain.Entities;

using Enums;
using ValueObjects;

/// <summary>
/// Represents an order in the system.
/// </summary>
public class Order : BaseEntity
{
    /// <summary>Gets the tenant ID this order belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the customer ID.</summary>
    public Guid CustomerId { get; private set; }

    /// <summary>Gets the order number (human-readable identifier).</summary>
    public string OrderNumber { get; private set; } = string.Empty;

    /// <summary>Gets the order status.</summary>
    public OrderStatus Status { get; private set; }

    /// <summary>Gets the subtotal amount.</summary>
    public Money Subtotal { get; private set; }

    /// <summary>Gets the tax amount.</summary>
    public Money TaxAmount { get; private set; }

    /// <summary>Gets the shipping cost.</summary>
    public Money ShippingCost { get; private set; }

    /// <summary>Gets the total amount.</summary>
    public Money Total { get; private set; }

    /// <summary>Gets the shipping address.</summary>
    public Address? ShippingAddress { get; private set; }

    /// <summary>Gets the billing address.</summary>
    public Address? BillingAddress { get; private set; }

    /// <summary>Gets the payment status.</summary>
    public PaymentStatus PaymentStatus { get; private set; }

    /// <summary>Gets the payment gateway reference.</summary>
    public string? PaymentGatewayReference { get; private set; }

    /// <summary>Gets the payment method used.</summary>
    public string? PaymentMethod { get; private set; }

    /// <summary>Gets the notes for the order.</summary>
    public string? Notes { get; private set; }

    /// <summary>Gets the tracking number (if shipped).</summary>
    public string? TrackingNumber { get; private set; }

    private List<OrderItem> _items = new();

    /// <summary>Gets the order items.</summary>
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Creates a new instance of Order.
    /// </summary>
    public static Order Create(
        Guid tenantId,
        Guid customerId,
        Address shippingAddress,
        Address billingAddress)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID is required.", nameof(customerId));

        var order = new Order
        {
            TenantId = tenantId,
            CustomerId = customerId,
            OrderNumber = GenerateOrderNumber(),
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            ShippingAddress = shippingAddress,
            BillingAddress = billingAddress,
            Subtotal = new Money(0),
            TaxAmount = new Money(0),
            ShippingCost = new Money(0),
            Total = new Money(0)
        };

        return order;
    }

    /// <summary>
    /// Adds an item to the order.
    /// </summary>
    public void AddItem(Guid productId, int quantity, Money unitPrice)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID is required.", nameof(productId));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        if (unitPrice.Amount <= 0)
            throw new ArgumentException("Unit price must be greater than zero.", nameof(unitPrice));

        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.UpdateQuantity(existingItem.Quantity + quantity);
        }
        else
        {
            var item = OrderItem.Create(productId, quantity, unitPrice);
            _items.Add(item);
        }
    }

    /// <summary>
    /// Updates the totals for the order.
    /// </summary>
    public void UpdateTotals(Money subtotal, Money taxAmount, Money shippingCost)
    {
        Subtotal = subtotal;
        TaxAmount = taxAmount;
        ShippingCost = shippingCost;
        Total = new Money(subtotal.Amount + taxAmount.Amount + shippingCost.Amount);
    }

    /// <summary>
    /// Confirms the order.
    /// </summary>
    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Only pending orders can be confirmed.");

        Status = OrderStatus.Confirmed;
    }

    /// <summary>
    /// Records payment for the order.
    /// </summary>
    public void RecordPayment(string paymentMethod, string? gatewayReference = null)
    {
        if (PaymentStatus == PaymentStatus.Completed)
            throw new InvalidOperationException("Payment already recorded.");

        PaymentMethod = paymentMethod;
        PaymentGatewayReference = gatewayReference;
        PaymentStatus = PaymentStatus.Completed;
        Status = OrderStatus.Paid;
    }

    /// <summary>
    /// Marks the order as processing.
    /// </summary>
    public void MarkAsProcessing()
    {
        if (Status != OrderStatus.Paid)
            throw new InvalidOperationException("Only paid orders can be marked as processing.");

        Status = OrderStatus.Processing;
    }

    /// <summary>
    /// Marks the order as shipped.
    /// </summary>
    public void MarkAsShipped(string trackingNumber)
    {
        if (Status != OrderStatus.Processing)
            throw new InvalidOperationException("Only processing orders can be marked as shipped.");

        TrackingNumber = trackingNumber;
        Status = OrderStatus.Shipped;
    }

    /// <summary>
    /// Marks the order as delivered.
    /// </summary>
    public void MarkAsDelivered()
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOperationException("Only shipped orders can be marked as delivered.");

        Status = OrderStatus.Delivered;
    }

    /// <summary>
    /// Cancels the order.
    /// </summary>
    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Order is already cancelled.");

        Status = OrderStatus.Cancelled;
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }
}
