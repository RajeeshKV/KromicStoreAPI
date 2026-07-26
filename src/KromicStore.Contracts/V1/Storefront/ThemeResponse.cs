namespace KromicStore.Contracts.V1.Storefront;

using System.Text.Json.Serialization;

/// <summary>
/// Response object representing a theme template.
/// Themes are platform-wide resources that tenants can use to create storefronts.
/// </summary>
public class ThemeResponse
{
    /// <summary>Gets the theme ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets the unique slug identifier for the theme (e.g., "minimal", "modern", "pro").</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets the display name of the theme.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the description of the theme.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets the semantic version of the theme.</summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>Gets a value indicating whether this theme is active and available for use.</summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets the complete theme definition stored as a JSON object.
    /// Contains default pages, sections, components, branding, navigation, and footer settings.
    /// </summary>
    [JsonPropertyName("definition")]
    public object? Definition { get; set; }

    /// <summary>Gets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets the last update timestamp.</summary>
    public DateTime? UpdatedAt { get; set; }
}
