namespace KromicStore.Contracts.V1.Storefront;

using System.Text.Json.Serialization;

/// <summary>
/// Response object representing a storefront component within a section.
/// </summary>
public class StorefrontComponentResponse
{
    /// <summary>Gets the component ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets the component type (e.g., "hero", "product-grid", "testimonial").</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets the component configuration as a JSON object.</summary>
    [JsonPropertyName("config")]
    public object? Config { get; set; }

    /// <summary>Gets a value indicating whether the component is visible.</summary>
    public bool IsVisible { get; set; }

    /// <summary>Gets the display order within the section.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Gets the optional CSS class for styling.</summary>
    public string? CssClass { get; set; }

    /// <summary>Gets the optional tracking ID for analytics.</summary>
    public string? TrackingId { get; set; }
}
