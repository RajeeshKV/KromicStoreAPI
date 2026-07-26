namespace KromicStore.Domain.Entities;

using Enums;
using ValueObjects;

/// <summary>
/// Represents a product in the catalog.
/// </summary>
public class Product : BaseEntity
{
    /// <summary>Gets the tenant ID this product belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the SKU (stock keeping unit).</summary>
    public string Sku { get; private set; } = string.Empty;

    /// <summary>Gets the product name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the product description.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Gets the product price.</summary>
    public Money Price { get; private set; }

    /// <summary>Gets the cost price.</summary>
    public Money? CostPrice { get; private set; }

    /// <summary>Gets the quantity in stock.</summary>
    public int StockQuantity { get; private set; }

    /// <summary>Gets the product status.</summary>
    public ProductStatus Status { get; private set; }

    /// <summary>Gets the category ID.</summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>Gets the primary image URL.</summary>
    public string? ImageUrl { get; private set; }

    /// <summary>Gets the weight in kilograms.</summary>
    public decimal? Weight { get; private set; }

    /// <summary>Gets the tax percentage applicable.</summary>
    public decimal TaxPercentage { get; private set; }

    /// <summary>Gets the reorder level threshold.</summary>
    public int ReorderLevel { get; private set; }

    /// <summary>
    /// Creates a new instance of Product.
    /// </summary>
    public static Product Create(
        Guid tenantId,
        string sku,
        string name,
        string description,
        Money price,
        int stockQuantity,
        Guid? categoryId = null,
        int reorderLevel = 10)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU is required.", nameof(sku));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));
        if (price.Amount <= 0)
            throw new ArgumentException("Price must be greater than zero.", nameof(price));
        if (stockQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative.", nameof(stockQuantity));

        return new Product
        {
            TenantId = tenantId,
            Sku = sku,
            Name = name,
            Description = description,
            Price = price,
            StockQuantity = stockQuantity,
            Status = ProductStatus.Draft,
            CategoryId = categoryId,
            TaxPercentage = 18,
            ReorderLevel = reorderLevel
        };
    }

    /// <summary>
    /// Updates product information.
    /// </summary>
    public void Update(string name, string description, Money price, Guid? categoryId = null)
    {
        Name = name;
        Description = description;
        Price = price;
        CategoryId = categoryId;
    }

    /// <summary>
    /// Updates stock quantity.
    /// </summary>
    public void UpdateStock(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative.", nameof(quantity));

        StockQuantity = quantity;
    }

    /// <summary>
    /// Reduces stock by the specified amount.
    /// </summary>
    public void ReduceStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity to reduce must be positive.", nameof(quantity));

        if (StockQuantity < quantity)
            throw new InvalidOperationException("Insufficient stock.");

        StockQuantity -= quantity;
    }

    /// <summary>
    /// Restores stock by the specified amount (used for order cancellations, returns, etc).
    /// </summary>
    public void RestoreStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity to restore must be positive.", nameof(quantity));

        StockQuantity += quantity;
    }

    /// <summary>
    /// Publishes the product (makes it available for purchase).
    /// </summary>
    public void Publish()
    {
        if (Status == ProductStatus.Active)
            throw new InvalidOperationException("Product is already published.");

        if (StockQuantity <= 0)
            throw new InvalidOperationException("Cannot publish product with zero or negative stock.");

        Status = ProductStatus.Active;
    }

    /// <summary>
    /// Unpublishes the product (makes it unavailable for purchase).
    /// </summary>
    public void Unpublish()
    {
        if (Status != ProductStatus.Active)
            throw new InvalidOperationException("Only active products can be unpublished.");

        Status = ProductStatus.Inactive;
    }

    /// <summary>
    /// Archives the product.
    /// </summary>
    public void Archive()
    {
        Status = ProductStatus.Archived;
    }

    /// <summary>
    /// Unassigns the product from its category (sets CategoryId to null).
    /// </summary>
    public void UnassignCategory()
    {
        CategoryId = null;
    }
}
