namespace KromicStore.Contracts.V1.Storefront;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request to create a new storefront from an existing theme template.
/// </summary>
public class CreateStorefrontFromThemeRequest
{
    /// <summary>
    /// Gets or sets the theme ID to use as template.
    /// </summary>
    [Required(ErrorMessage = "Theme ID is required")]
    public Guid ThemeId { get; set; }

    /// <summary>
    /// Gets or sets the storefront name/title.
    /// </summary>
    [Required(ErrorMessage = "Store name is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Store name must be between 1 and 200 characters")]
    public string StoreName { get; set; } = string.Empty;
}
