namespace KromicStore.Domain.Entities;

using ValueObjects;

/// <summary>
/// Represents an item in an order.
/// </summary>
public class OrderItem : BaseEntity
{
    /// <summary>Gets the product ID.</summary>
    public Guid ProductId { get; private set; }

    /// <summary>Gets the product name at the time of order.</summary>
    public string ProductName { get; private set; } = string.Empty;

    /// <summary>Gets the product SKU at the time of order (snapshot).</summary>
    public string ProductSku { get; private set; } = string.Empty;

    /// <summary>Gets the quantity ordered.</summary>
    public int Quantity { get; private set; }

    /// <summary>Gets the unit price at the time of order.</summary>
    public Money UnitPrice { get; private set; }

    /// <summary>Gets the total price for this item.</summary>
    public Money TotalPrice { get; private set; }

    /// <summary>
    /// Creates a new instance of OrderItem.
    /// </summary>
    public static OrderItem Create(
        Guid productId,
        int quantity,
        Money unitPrice,
        string productName = "",
        string productSku = "")
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID is required.", nameof(productId));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        if (unitPrice.Amount <= 0)
            throw new ArgumentException("Unit price must be greater than zero.", nameof(unitPrice));

        return new OrderItem
        {
            ProductId = productId,
            ProductName = productName,
            ProductSku = productSku,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalPrice = new Money(unitPrice.Amount * quantity)
        };
    }

    /// <summary>
    /// Updates the quantity for this item.
    /// </summary>
    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));

        Quantity = quantity;
        TotalPrice = new Money(UnitPrice.Amount * quantity);
    }
}
