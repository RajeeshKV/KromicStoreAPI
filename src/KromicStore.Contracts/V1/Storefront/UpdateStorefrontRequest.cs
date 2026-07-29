namespace KromicStore.Contracts.V1.Storefront;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request to update storefront metadata and configuration.
/// </summary>
public class UpdateStorefrontRequest
{
    /// <summary>
    /// Gets or sets the storefront name.
    /// </summary>
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Store name must be between 1 and 200 characters")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the logo URL.
    /// </summary>
    [Url(ErrorMessage = "Logo URL must be a valid URL")]
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Gets or sets the contact email address.
    /// </summary>
    [EmailAddress(ErrorMessage = "Contact email must be a valid email address")]
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Gets or sets the contact phone number.
    /// </summary>
    [Phone(ErrorMessage = "Contact phone must be a valid phone number")]
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Gets or sets the store address.
    /// </summary>
    [StringLength(500, ErrorMessage = "Address must not exceed 500 characters")]
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the store currency code (ISO 4217).
    /// </summary>
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a valid ISO 4217 code")]
    public string? Currency { get; set; }

    /// <summary>
    /// Gets or sets the store country code (ISO 3166-1).
    /// </summary>
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Country must be a valid ISO 3166-1 code")]
    public string? Country { get; set; }

    /// <summary>
    /// Gets or sets the brand primary color in hexadecimal format.
    /// </summary>
    [RegularExpression(@"^#?[0-9A-Fa-f]{6}$", ErrorMessage = "Brand color must be a valid hex color code")]
    public string? BrandColor { get; set; }

    /// <summary>
    /// Gets or sets the copyright text.
    /// </summary>
    [StringLength(500, ErrorMessage = "Copyright text must not exceed 500 characters")]
    public string? Copyright { get; set; }

    /// <summary>
    /// Gets or sets the Facebook URL for footer social links.
    /// </summary>
    [Url(ErrorMessage = "Facebook URL must be a valid URL")]
    public string? FacebookUrl { get; set; }

    /// <summary>
    /// Gets or sets the Twitter/X URL for footer social links.
    /// </summary>
    [Url(ErrorMessage = "Twitter URL must be a valid URL")]
    public string? TwitterUrl { get; set; }

    /// <summary>
    /// Gets or sets the Instagram URL for footer social links.
    /// </summary>
    [Url(ErrorMessage = "Instagram URL must be a valid URL")]
    public string? InstagramUrl { get; set; }

    /// <summary>
    /// Gets or sets the LinkedIn URL for footer social links.
    /// </summary>
    [Url(ErrorMessage = "LinkedIn URL must be a valid URL")]
    public string? LinkedInUrl { get; set; }
}
