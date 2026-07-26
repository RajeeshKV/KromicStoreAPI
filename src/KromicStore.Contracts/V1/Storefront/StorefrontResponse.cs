namespace KromicStore.Contracts.V1.Storefront;

/// <summary>
/// Response object representing a complete storefront entity.
/// </summary>
public class StorefrontResponse
{
    /// <summary>Gets the storefront ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets the storefront name/title.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the storefront status (Draft, Published, Archived).</summary>
    public string Status { get; set; } = "Draft";

    /// <summary>Gets the optional theme ID applied to this storefront.</summary>
    public Guid? ThemeId { get; set; }

    /// <summary>Gets the logo URL.</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Gets the contact email address.</summary>
    public string? ContactEmail { get; set; }

    /// <summary>Gets the contact phone number.</summary>
    public string? ContactPhone { get; set; }

    /// <summary>Gets the store address.</summary>
    public string? Address { get; set; }

    /// <summary>Gets the store currency code.</summary>
    public string Currency { get; set; } = "INR";

    /// <summary>Gets the store country code.</summary>
    public string? Country { get; set; }

    /// <summary>Gets the brand primary color in hexadecimal format.</summary>
    public string? BrandColor { get; set; }

    /// <summary>Gets the copyright text.</summary>
    public string? Copyright { get; set; }

    /// <summary>Gets the mandatory fields status (which required fields are still placeholders).</summary>
    public MandatoryFieldsStatusResponse MandatoryFieldsStatus { get; set; } = new();

    /// <summary>Gets the pages in this storefront.</summary>
    public List<StorefrontPageResponse> Pages { get; set; } = new();

    /// <summary>Gets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets the last update timestamp.</summary>
    public DateTime? UpdatedAt { get; set; }
}
