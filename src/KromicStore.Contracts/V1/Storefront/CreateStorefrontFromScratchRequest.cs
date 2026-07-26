namespace KromicStore.Contracts.V1.Storefront;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request to create a new storefront from scratch without a theme template.
/// </summary>
public class CreateStorefrontFromScratchRequest
{
    /// <summary>
    /// Gets or sets the storefront name/title.
    /// </summary>
    [Required(ErrorMessage = "Store name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Store name must be between 1 and 100 characters")]
    public string StoreName { get; set; } = string.Empty;
}
