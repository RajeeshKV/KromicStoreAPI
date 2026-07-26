namespace KromicStore.Contracts.V1.Storefront;

/// <summary>
/// Response object representing the status of mandatory storefront fields.
/// Tracks which required fields are still placeholders (not provided by user).
/// </summary>
public class MandatoryFieldsStatusResponse
{
    /// <summary>Gets a value indicating whether the store name field is a placeholder.</summary>
    public bool StoreNameIsPlaceholder { get; set; }

    /// <summary>Gets a value indicating whether the logo field is a placeholder.</summary>
    public bool LogoIsPlaceholder { get; set; }

    /// <summary>Gets a value indicating whether the contact email field is a placeholder.</summary>
    public bool EmailIsPlaceholder { get; set; }

    /// <summary>Gets a value indicating whether the contact phone field is a placeholder.</summary>
    public bool PhoneIsPlaceholder { get; set; }

    /// <summary>Gets a value indicating whether the address field is a placeholder.</summary>
    public bool AddressIsPlaceholder { get; set; }

    /// <summary>Gets a value indicating whether the currency field is a placeholder.</summary>
    public bool CurrencyIsPlaceholder { get; set; }

    /// <summary>Gets a value indicating whether the country field is a placeholder.</summary>
    public bool CountryIsPlaceholder { get; set; }

    /// <summary>Gets a value indicating whether the brand color field is a placeholder.</summary>
    public bool BrandColorIsPlaceholder { get; set; }

    /// <summary>Gets a value indicating whether the copyright field is a placeholder.</summary>
    public bool CopyrightIsPlaceholder { get; set; }

    /// <summary>
    /// Gets a value indicating whether all mandatory fields have been provided (none are placeholders).
    /// </summary>
    public bool AllFieldsProvided =>
        !StoreNameIsPlaceholder &&
        !LogoIsPlaceholder &&
        !EmailIsPlaceholder &&
        !PhoneIsPlaceholder &&
        !AddressIsPlaceholder &&
        !CurrencyIsPlaceholder &&
        !CountryIsPlaceholder &&
        !BrandColorIsPlaceholder &&
        !CopyrightIsPlaceholder;
}
