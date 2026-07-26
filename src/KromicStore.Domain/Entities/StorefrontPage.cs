namespace KromicStore.Domain.Entities;

/// <summary>
/// Represents a page within a storefront.
/// </summary>
public class StorefrontPage : BaseEntity
{
    /// <summary>Gets the tenant ID this page belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the parent storefront ID.</summary>
    public Guid StorefrontId { get; private set; }

    /// <summary>Gets the parent storefront.</summary>
    public Storefront? Storefront { get; private set; }

    /// <summary>Gets the page name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the page slug (URL-friendly identifier).</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>Gets the page description/meta description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the page visibility status.</summary>
    public PageVisibility Visibility { get; private set; } = PageVisibility.Draft;

    /// <summary>Gets the page layout type.</summary>
    public string LayoutType { get; private set; } = "default";

    /// <summary>Gets the display order for navigation.</summary>
    public int DisplayOrder { get; private set; }

    /// <summary>Gets optional meta keywords for SEO.</summary>
    public string? MetaKeywords { get; private set; }

    /// <summary>Gets a value indicating whether the page is featured/pinned.</summary>
    public bool IsFeatured { get; private set; }

    /// <summary>Gets the sections in this page.</summary>
    public ICollection<StorefrontSection> Sections { get; private set; } = new List<StorefrontSection>();

    /// <summary>
    /// Creates a new instance of StorefrontPage.
    /// </summary>
    public static StorefrontPage Create(
        Guid tenantId,
        Guid storefrontId,
        string name,
        string slug,
        int displayOrder = 0,
        string? description = null,
        string layoutType = "default")
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (storefrontId == Guid.Empty)
            throw new ArgumentException("Storefront ID is required.", nameof(storefrontId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Page name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Page slug is required.", nameof(slug));

        return new StorefrontPage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StorefrontId = storefrontId,
            Name = name,
            Slug = slug.ToLowerInvariant().Replace(" ", "-"),
            Description = description,
            DisplayOrder = displayOrder,
            LayoutType = layoutType,
            Visibility = PageVisibility.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates page information.
    /// </summary>
    public void Update(string name, string slug, string? description = null, 
        string? layoutType = null, string? metaKeywords = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name;

        if (!string.IsNullOrWhiteSpace(slug))
            Slug = slug.ToLowerInvariant().Replace(" ", "-");

        Description = description;

        if (!string.IsNullOrWhiteSpace(layoutType))
            LayoutType = layoutType;

        MetaKeywords = metaKeywords;

        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the display order.
    /// </summary>
    public void SetDisplayOrder(int order)
    {
        if (order < 0)
            throw new ArgumentException("Display order cannot be negative.", nameof(order));

        DisplayOrder = order;
        UpdateTimestamp();
    }

    /// <summary>
    /// Publishes the page (makes it visible to customers).
    /// </summary>
    public void Publish()
    {
        if (Visibility == PageVisibility.Published)
            throw new InvalidOperationException("Page is already published.");

        Visibility = PageVisibility.Published;
        UpdateTimestamp();
    }

    /// <summary>
    /// Unpublishes the page (hides from customers).
    /// </summary>
    public void Unpublish()
    {
        if (Visibility != PageVisibility.Published)
            throw new InvalidOperationException("Only published pages can be unpublished.");

        Visibility = PageVisibility.Draft;
        UpdateTimestamp();
    }

    /// <summary>
    /// Archives the page.
    /// </summary>
    public void Archive()
    {
        if (Visibility == PageVisibility.Archived)
            throw new InvalidOperationException("Page is already archived.");

        Visibility = PageVisibility.Archived;
        UpdateTimestamp();
    }

    /// <summary>
    /// Marks or unmarks the page as featured.
    /// </summary>
    public void SetFeatured(bool featured)
    {
        IsFeatured = featured;
        UpdateTimestamp();
    }

    /// <summary>
    /// Adds a section to the page.
    /// </summary>
    public void AddSection(StorefrontSection section)
    {
        if (section == null)
            throw new ArgumentNullException(nameof(section));
        if (section.TenantId != TenantId)
            throw new InvalidOperationException("Section must belong to the same tenant.");
        if (section.PageId != Id)
            throw new InvalidOperationException("Section page ID must match this page.");

        Sections.Add(section);
        UpdateTimestamp();
    }

    /// <summary>
    /// Removes a section from the page.
    /// </summary>
    public void RemoveSection(Guid sectionId)
    {
        var section = Sections.FirstOrDefault(s => s.Id == sectionId);
        if (section != null)
        {
            Sections.Remove(section);
            UpdateTimestamp();
        }
    }

    /// <summary>
    /// Gets a section by ID.
    /// </summary>
    public StorefrontSection? GetSection(Guid sectionId)
    {
        return Sections.FirstOrDefault(s => s.Id == sectionId);
    }

    /// <summary>
    /// Gets all visible sections ordered by display order.
    /// </summary>
    public IEnumerable<StorefrontSection> GetVisibleSections()
    {
        return Sections.Where(s => s.IsVisible).OrderBy(s => s.DisplayOrder);
    }

    /// <summary>
    /// Updates section order.
    /// </summary>
    public void ReorderSections(IDictionary<Guid, int> sectionOrders)
    {
        if (sectionOrders == null)
            throw new ArgumentNullException(nameof(sectionOrders));

        foreach (var section in Sections)
        {
            if (sectionOrders.TryGetValue(section.Id, out var newOrder))
            {
                section.SetDisplayOrder(newOrder);
            }
        }

        UpdateTimestamp();
    }
}

/// <summary>
/// Enumeration of page visibility states.
/// </summary>
public enum PageVisibility
{
    /// <summary>Page is in draft state and not visible to customers.</summary>
    Draft = 1,

    /// <summary>Page is published and visible to customers.</summary>
    Published = 2,

    /// <summary>Page is archived and no longer visible.</summary>
    Archived = 3
}
