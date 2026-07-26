#nullable disable

namespace KromicStore.Contracts.V1.Products;

/// <summary>
/// Response DTO for a product category.
/// </summary>
public class CategoryResponse
{
    /// <summary>
    /// The category ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The category name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The category description.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// The parent category ID (if this is a subcategory).
    /// </summary>
    public Guid? ParentCategoryId { get; set; }

    /// <summary>
    /// Display order for UI sorting.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// The nesting level (0 for top-level, 1 for second-level, 2 for third-level).
    /// </summary>
    public int NestingLevel { get; set; }

    /// <summary>
    /// The count of direct subcategories.
    /// </summary>
    public int SubcategoryCount { get; set; }

    /// <summary>
    /// The count of products directly assigned to this category.
    /// </summary>
    public int ProductCount { get; set; }

    /// <summary>
    /// Nested subcategories with their own hierarchy.
    /// </summary>
    public List<CategoryResponse> Subcategories { get; set; } = new();

    /// <summary>
    /// The date and time when the category was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// The date and time when the category was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Response DTO for a paginated list of categories.
/// </summary>
public class CategoryListResponse
{
    /// <summary>
    /// The list of categories (tree structure with nested subcategories).
    /// </summary>
    public List<CategoryResponse> Data { get; set; } = new();

    /// <summary>
    /// The total count of top-level categories.
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// Request DTO for reordering a category.
/// </summary>
public class ReorderCategoryRequest
{
    /// <summary>
    /// The new display order for the category.
    /// </summary>
    public int DisplayOrder { get; set; }
}
