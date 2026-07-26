#nullable disable

namespace KromicStore.Contracts.V1.Products;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for creating a new product category.
/// </summary>
public class CreateCategoryRequest
{
    /// <summary>
    /// The category name.
    /// </summary>
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(200, MinimumLength = 1, 
        ErrorMessage = "Category name must be between 1 and 200 characters")]
    public string Name { get; set; }

    /// <summary>
    /// Optional category description.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string Description { get; set; }

    /// <summary>
    /// Optional parent category ID for hierarchical organization (supports up to 3 levels).
    /// </summary>
    public Guid? ParentCategoryId { get; set; }

    /// <summary>
    /// Display order for UI sorting (default: 0).
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Display order must be non-negative")]
    public int DisplayOrder { get; set; } = 0;
}
