namespace KromicStore.Contracts.V1.Storefront;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request to create a new theme.
/// </summary>
public class CreateThemeRequest
{
    /// <summary>Gets or sets the theme name.</summary>
    [Required(ErrorMessage = "Theme name is required")]
    [StringLength(255, ErrorMessage = "Theme name cannot exceed 255 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the theme description.</summary>
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    /// <summary>Gets or sets the complete theme definition as JSON (optional, uses default if not provided).</summary>
    public string? DefinitionJson { get; set; }

    /// <summary>Gets or sets the semantic version of the theme (optional, defaults to 1.0.0).</summary>
    [StringLength(20, ErrorMessage = "Version cannot exceed 20 characters")]
    public string? Version { get; set; }

    /// <summary>Gets or sets a value indicating whether this theme should be public (shared with other tenants).</summary>
    public bool IsPublic { get; set; } = false;
}
