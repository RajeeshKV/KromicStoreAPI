#nullable disable

namespace KromicStore.Contracts.V1.Products;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for creating a new product.
/// </summary>
public class CreateProductRequest
{
    /// <summary>
    /// The SKU (Stock Keeping Unit) - must be unique within the tenant.
    /// </summary>
    [Required(ErrorMessage = "SKU is required")]
    [StringLength(50, MinimumLength = 1, 
        ErrorMessage = "SKU must be between 1 and 50 characters")]
    public string Sku { get; set; }

    /// <summary>
    /// The product name visible to customers.
    /// </summary>
    [Required(ErrorMessage = "Product name is required")]
    [StringLength(200, MinimumLength = 1, 
        ErrorMessage = "Product name must be between 1 and 200 characters")]
    public string Name { get; set; }

    /// <summary>
    /// Detailed product description.
    /// </summary>
    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string Description { get; set; }

    /// <summary>
    /// The selling price of the product.
    /// </summary>
    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, double.MaxValue, 
        ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    /// <summary>
    /// The quantity of the product in stock.
    /// </summary>
    [Required(ErrorMessage = "Stock quantity is required")]
    [Range(0, int.MaxValue, 
        ErrorMessage = "Stock quantity cannot be negative")]
    public int StockQuantity { get; set; }

    /// <summary>
    /// The category ID the product belongs to (optional).
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// The cost price of the product (optional).
    /// </summary>
    [Range(0, double.MaxValue, 
        ErrorMessage = "Cost price cannot be negative")]
    public decimal? CostPrice { get; set; }

    /// <summary>
    /// The product images (optional).
    /// Each image should be uploaded first via the media endpoint, then provide the URL and public ID.
    /// </summary>
    public List<ProductImageRequest>? Images { get; set; }
}

/// <summary>
/// Request DTO for a product image.
/// </summary>
public class ProductImageRequest
{
    /// <summary>
    /// The Cloudinary URL of the image.
    /// </summary>
    [Required(ErrorMessage = "Image URL is required")]
    [Url(ErrorMessage = "Image URL must be a valid URL")]
    public string Url { get; set; }

    /// <summary>
    /// The Cloudinary public ID for deletion/management.
    /// </summary>
    [Required(ErrorMessage = "Cloudinary public ID is required")]
    public string CloudinaryPublicId { get; set; }

    /// <summary>
    /// The display order for sorting images.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this is the primary/featured image.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// The alt text for accessibility.
    /// </summary>
    [StringLength(500, ErrorMessage = "Alt text cannot exceed 500 characters")]
    public string? AltText { get; set; }
}
