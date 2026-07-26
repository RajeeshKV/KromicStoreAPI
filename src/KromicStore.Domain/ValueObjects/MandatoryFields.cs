namespace KromicStore.Domain.ValueObjects;

/// <summary>
/// Represents tracking of mandatory storefront fields.
/// Tracks which merchant-provided mandatory fields are actual data vs. placeholders.
/// </summary>
public record MandatoryFields
{
    /// <summary>
    /// Gets a value indicating whether StoreName is a placeholder.
    /// </summary>
    public bool IsStoreNamePlaceholder { get; init; }

    /// <summary>
    /// Gets a value indicating whether Logo is a placeholder.
    /// </summary>
    public bool IsLogoPlaceholder { get; init; }

    /// <summary>
    /// Gets a value indicating whether Email is a placeholder.
    /// </summary>
    public bool IsEmailPlaceholder { get; init; }

    /// <summary>
    /// Gets a value indicating whether Phone is a placeholder.
    /// </summary>
    public bool IsPhonePlaceholder { get; init; }

    /// <summary>
    /// Gets a value indicating whether Address is a placeholder.
    /// </summary>
    public bool IsAddressPlaceholder { get; init; }

    /// <summary>
    /// Gets a value indicating whether Currency is a placeholder.
    /// </summary>
    public bool IsCurrencyPlaceholder { get; init; }

    /// <summary>
    /// Gets a value indicating whether Country is a placeholder.
    /// </summary>
    public bool IsCountryPlaceholder { get; init; }

    /// <summary>
    /// Gets a value indicating whether BrandColor is a placeholder.
    /// </summary>
    public bool IsBrandColorPlaceholder { get; init; }

    /// <summary>
    /// Gets a value indicating whether Copyright is a placeholder.
    /// </summary>
    public bool IsCopyrightPlaceholder { get; init; }

    /// <summary>
    /// Creates a new instance with all fields as placeholders (default state for new storefronts).
    /// </summary>
    public static MandatoryFields CreateAllPlaceholders()
    {
        return new MandatoryFields
        {
            IsStoreNamePlaceholder = true,
            IsLogoPlaceholder = true,
            IsEmailPlaceholder = true,
            IsPhonePlaceholder = true,
            IsAddressPlaceholder = true,
            IsCurrencyPlaceholder = true,
            IsCountryPlaceholder = true,
            IsBrandColorPlaceholder = true,
            IsCopyrightPlaceholder = true
        };
    }

    /// <summary>
    /// Gets the count of provided (non-placeholder) fields.
    /// </summary>
    public int GetProvidedFieldCount()
    {
        var count = 0;
        if (!IsStoreNamePlaceholder) count++;
        if (!IsLogoPlaceholder) count++;
        if (!IsEmailPlaceholder) count++;
        if (!IsPhonePlaceholder) count++;
        if (!IsAddressPlaceholder) count++;
        if (!IsCurrencyPlaceholder) count++;
        if (!IsCountryPlaceholder) count++;
        if (!IsBrandColorPlaceholder) count++;
        if (!IsCopyrightPlaceholder) count++;
        return count;
    }

    /// <summary>
    /// Checks if all mandatory fields have been provided by the merchant.
    /// </summary>
    public bool AreAllFieldsProvided()
    {
        return !IsStoreNamePlaceholder &&
               !IsLogoPlaceholder &&
               !IsEmailPlaceholder &&
               !IsPhonePlaceholder &&
               !IsAddressPlaceholder &&
               !IsCurrencyPlaceholder &&
               !IsCountryPlaceholder &&
               !IsBrandColorPlaceholder &&
               !IsCopyrightPlaceholder;
    }

    /// <summary>
    /// Creates a new instance by updating a specific field's placeholder status.
    /// </summary>
    public MandatoryFields WithFieldUpdated(string fieldName, bool isPlaceholder)
    {
        return fieldName?.ToLowerInvariant() switch
        {
            "storename" => this with { IsStoreNamePlaceholder = isPlaceholder },
            "logo" => this with { IsLogoPlaceholder = isPlaceholder },
            "email" => this with { IsEmailPlaceholder = isPlaceholder },
            "phone" => this with { IsPhonePlaceholder = isPlaceholder },
            "address" => this with { IsAddressPlaceholder = isPlaceholder },
            "currency" => this with { IsCurrencyPlaceholder = isPlaceholder },
            "country" => this with { IsCountryPlaceholder = isPlaceholder },
            "brandcolor" => this with { IsBrandColorPlaceholder = isPlaceholder },
            "copyright" => this with { IsCopyrightPlaceholder = isPlaceholder },
            _ => this
        };
    }
}
