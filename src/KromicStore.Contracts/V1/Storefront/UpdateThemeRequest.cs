namespace KromicStore.Contracts.V1.Storefront;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request to update an existing theme.
/// </summary>
public class UpdateThemeRequest
{
    /// <summary>Gets or sets the theme name (optional).</summary>
    [StringLength(255, ErrorMessage = "Theme name cannot exceed 255 characters")]
    public string? Name { get; set; }

    /// <summary>Gets or sets the theme description (optional).</summary>
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    /// <summary>Gets or sets the complete theme definition as JSON (optional).</summary>
    public string? DefinitionJson { get; set; }

    /// <summary>Gets or sets the semantic version of the theme (optional).</summary>
    [StringLength(20, ErrorMessage = "Version cannot exceed 20 characters")]
    public string? Version { get; set; }
}
