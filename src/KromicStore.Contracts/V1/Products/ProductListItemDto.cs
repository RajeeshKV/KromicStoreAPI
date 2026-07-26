namespace KromicStore.Contracts.V1.Products;

/// <summary>
/// Represents a product item in a list response.
/// Contains essential product information for list views.
/// </summary>
public record ProductListItemDto(
    /// <summary>
    /// The unique identifier of the product.
    /// </summary>
    Guid Id,
    
    /// <summary>
    /// The product name.
    /// </summary>
    string Name,
    
    /// <summary>
    /// The SKU (Stock Keeping Unit).
    /// </summary>
    string Sku,
    
    /// <summary>
    /// The product price.
    /// </summary>
    decimal Price,
    
    /// <summary>
    /// The current quantity in stock.
    /// </summary>
    int StockQuantity,
    
    /// <summary>
    /// The current status of the product.
    /// </summary>
    string Status,
    
    /// <summary>
    /// The URL to the product's primary image (optional).
    /// </summary>
    string? ImageUrl);
