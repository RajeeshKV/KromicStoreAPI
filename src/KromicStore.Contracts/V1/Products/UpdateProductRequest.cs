#nullable disable

namespace KromicStore.Contracts.V1.Products;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for updating an existing product.
/// </summary>
public class UpdateProductRequest
{
    /// <summary>
    /// The updated product name.
    /// </summary>
    [Required(ErrorMessage = "Product name is required")]
    [StringLength(200, MinimumLength = 1, 
        ErrorMessage = "Product name must be between 1 and 200 characters")]
    public string Name { get; set; }

    /// <summary>
    /// The updated product description.
    /// </summary>
    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string Description { get; set; }

    /// <summary>
    /// The updated selling price.
    /// </summary>
    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, double.MaxValue, 
        ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    /// <summary>
    /// The updated category ID (optional).
    /// </summary>
    public Guid? CategoryId { get; set; }
}
