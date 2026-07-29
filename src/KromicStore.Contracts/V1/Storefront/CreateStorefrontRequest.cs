namespace KromicStore.Contracts.V1.Storefront;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request to create a new storefront for the current tenant.
/// Since one tenant = one storefront, this is a unified create endpoint.
/// Can create from theme (ThemeId provided) or from scratch (ThemeId null).
/// </summary>
public class CreateStorefrontRequest
{
    /// <summary>
    /// Gets or sets the optional theme ID to use as template.
    /// If not provided, storefront is created from scratch with default theme.
    /// </summary>
    public string? ThemeId { get; set; }

    /// <summary>
    /// Gets or sets the storefront name/title.
    /// </summary>
    [Required(ErrorMessage = "Store name is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Store name must be between 1 and 200 characters")]
    public string StoreName { get; set; } = string.Empty;
}
