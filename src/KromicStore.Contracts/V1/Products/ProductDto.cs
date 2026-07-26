namespace KromicStore.Contracts.V1.Products;

/// <summary>
/// Represents product details in the response.
/// </summary>
public record ProductDto(
    /// <summary>
    /// The unique identifier of the product.
    /// </summary>
    Guid Id,
    
    /// <summary>
    /// The SKU (Stock Keeping Unit) - unique identifier within tenant.
    /// </summary>
    string Sku,
    
    /// <summary>
    /// The product name visible to customers.
    /// </summary>
    string Name,
    
    /// <summary>
    /// Detailed product description.
    /// </summary>
    string Description,
    
    /// <summary>
    /// The selling price of the product.
    /// </summary>
    decimal Price,
    
    /// <summary>
    /// The cost price of the product (optional).
    /// </summary>
    decimal? CostPrice,
    
    /// <summary>
    /// The current quantity in stock.
    /// </summary>
    int StockQuantity,
    
    /// <summary>
    /// The current status of the product (Draft, Published, Archived).
    /// </summary>
    string Status,
    
    /// <summary>
    /// The category ID the product belongs to (optional).
    /// </summary>
    Guid? CategoryId,
    
    /// <summary>
    /// The URL to the product's primary image (optional).
    /// </summary>
    string? ImageUrl,
    
    /// <summary>
    /// The weight of the product for shipping calculations (optional).
    /// </summary>
    decimal? Weight,
    
    /// <summary>
    /// The tax percentage applicable to this product.
    /// </summary>
    decimal TaxPercentage,
    
    /// <summary>
    /// The UTC timestamp when the product was created.
    /// </summary>
    DateTime CreatedAt,
    
    /// <summary>
    /// The UTC timestamp when the product was last updated.
    /// </summary>
    DateTime UpdatedAt);
