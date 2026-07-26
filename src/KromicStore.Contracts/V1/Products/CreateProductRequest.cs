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
}
