namespace KromicStore.Contracts.V1.Storefront;

/// <summary>
/// Response object representing a page within a storefront.
/// </summary>
public class StorefrontPageResponse
{
    /// <summary>Gets the page ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets the page name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the page slug (URL-friendly identifier).</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets the page description/meta description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets the page visibility status (Draft, Published, Archived).</summary>
    public string Visibility { get; set; } = "Draft";

    /// <summary>Gets the page layout type.</summary>
    public string LayoutType { get; set; } = "default";

    /// <summary>Gets the display order for navigation.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Gets optional meta keywords for SEO.</summary>
    public string? MetaKeywords { get; set; }

    /// <summary>Gets a value indicating whether the page is featured/pinned.</summary>
    public bool IsFeatured { get; set; }

    /// <summary>Gets the sections in this page.</summary>
    public List<StorefrontSectionResponse> Sections { get; set; } = new();
}
