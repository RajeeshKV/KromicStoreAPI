namespace KromicStore.Contracts.V1.Storefront;

/// <summary>
/// Response object representing a section within a storefront page.
/// A section is a container for multiple components organized together.
/// </summary>
public class StorefrontSectionResponse
{
    /// <summary>Gets the section ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets the section name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the section description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets a value indicating whether the section is visible.</summary>
    public bool IsVisible { get; set; }

    /// <summary>Gets the display order within the page.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Gets the optional CSS class for styling.</summary>
    public string? CssClass { get; set; }

    /// <summary>Gets the background color (hex code).</summary>
    public string? BackgroundColor { get; set; }

    /// <summary>Gets the background image URL.</summary>
    public string? BackgroundImageUrl { get; set; }

    /// <summary>Gets the components in this section.</summary>
    public List<StorefrontComponentResponse> Components { get; set; } = new();
}
