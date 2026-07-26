namespace KromicStore.Domain.Entities;

/// <summary>
/// Represents a product category in the catalog.
/// </summary>
public class Category : BaseEntity
{
    /// <summary>Gets the tenant ID this category belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the category name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the category description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the parent category ID for hierarchical organization.</summary>
    public Guid? ParentCategoryId { get; private set; }

    /// <summary>Gets the display order for UI sorting.</summary>
    public int DisplayOrder { get; private set; }

    /// <summary>Gets the nesting level (0 for top-level, max 2 for 3-level hierarchy).</summary>
    public int NestingLevel { get; private set; }

    private List<Guid> _subcategoryIds = new();

    /// <summary>Gets the IDs of direct subcategories.</summary>
    public IReadOnlyList<Guid> SubcategoryIds => _subcategoryIds.AsReadOnly();

    /// <summary>
    /// Creates a new instance of Category.
    /// </summary>
    public static Category Create(
        Guid tenantId,
        string name,
        string? description = null,
        Guid? parentCategoryId = null,
        int displayOrder = 0)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.", nameof(name));

        return new Category
        {
            TenantId = tenantId,
            Name = name,
            Description = description,
            ParentCategoryId = parentCategoryId,
            DisplayOrder = displayOrder,
            NestingLevel = parentCategoryId.HasValue ? 1 : 0
        };
    }

    /// <summary>
    /// Updates category information.
    /// </summary>
    public void Update(string name, string? description = null, int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.", nameof(name));

        Name = name;
        Description = description;
        DisplayOrder = displayOrder;
    }

    /// <summary>
    /// Sets the parent category with validation for circular references and nesting limits.
    /// </summary>
    public void SetParentCategory(Guid? parentCategoryId, IEnumerable<Category> allCategories)
    {
        if (!parentCategoryId.HasValue)
        {
            ParentCategoryId = null;
            NestingLevel = 0;
            return;
        }

        if (parentCategoryId == Id)
            throw new InvalidOperationException("A category cannot be its own parent.");

        var allCategoryList = allCategories.ToList();
        var parentCategory = allCategoryList.FirstOrDefault(c => c.Id == parentCategoryId);
        if (parentCategory == null)
            throw new InvalidOperationException("Parent category not found.");

        // Check for circular references (cannot have current category as ancestor of parent)
        var currentAncestor = parentCategory.ParentCategoryId;
        while (currentAncestor.HasValue)
        {
            if (currentAncestor == Id)
                throw new InvalidOperationException("Cannot create circular hierarchy. This parent is a descendant of the current category.");

            currentAncestor = allCategoryList
                .FirstOrDefault(c => c.Id == currentAncestor)
                ?.ParentCategoryId;
        }

        // Enforce max 3-level nesting (0, 1, 2)
        if (parentCategory.NestingLevel >= 2)
            throw new InvalidOperationException("Cannot exceed 3 levels of category nesting.");

        ParentCategoryId = parentCategoryId;
        NestingLevel = parentCategory.NestingLevel + 1;
    }

    /// <summary>
    /// Adds a subcategory ID reference.
    /// </summary>
    public void AddSubcategory(Guid subcategoryId)
    {
        if (!_subcategoryIds.Contains(subcategoryId))
        {
            _subcategoryIds.Add(subcategoryId);
        }
    }

    /// <summary>
    /// Removes a subcategory ID reference.
    /// </summary>
    public void RemoveSubcategory(Guid subcategoryId)
    {
        _subcategoryIds.Remove(subcategoryId);
    }
}
