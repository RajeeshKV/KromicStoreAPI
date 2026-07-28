namespace KromicStore.Domain.Entities;

using Enums;
using ValueObjects;

/// <summary>
/// Represents a storefront for a tenant.
/// Root aggregate for the storefront and theming system.
/// </summary>
public class Storefront : BaseEntity
{
    /// <summary>Gets the tenant ID this storefront belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the storefront name/title.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the storefront status.</summary>
    public StorefrontStatus Status { get; private set; } = StorefrontStatus.Draft;

    /// <summary>Gets the timestamp when the storefront was last published.</summary>
    public DateTime? PublishedAt { get; private set; }

    /// <summary>Gets the optional theme ID applied to this storefront.</summary>
    public Guid? ThemeId { get; private set; }

    /// <summary>Gets the logo URL.</summary>
    public string? LogoUrl { get; private set; }

    /// <summary>Gets the contact email address.</summary>
    public string? ContactEmail { get; private set; }

    /// <summary>Gets the contact phone number.</summary>
    public string? ContactPhone { get; private set; }

    /// <summary>Gets the store address.</summary>
    public string? Address { get; private set; }

    /// <summary>Gets the store currency.</summary>
    public string Currency { get; private set; } = "INR";

    /// <summary>Gets the store country.</summary>
    public string? Country { get; private set; }

    /// <summary>Gets the brand primary color (hex).</summary>
    public string? BrandColor { get; private set; }

    /// <summary>Gets the copyright text.</summary>
    public string? Copyright { get; private set; }

    /// <summary>Gets tracking of mandatory fields and whether they are placeholders.</summary>
    public MandatoryFields MandatoryFields { get; private set; } = MandatoryFields.CreateAllPlaceholders();

    /// <summary>Gets the pages in this storefront.</summary>
    public ICollection<StorefrontPage> Pages { get; private set; } = new List<StorefrontPage>();

    /// <summary>
    /// Creates a new storefront from scratch (without theme).
    /// </summary>
    public static Storefront CreateFromScratch(Guid tenantId, string name)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Storefront name is required.", nameof(name));

        return new Storefront
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Status = StorefrontStatus.Draft,
            MandatoryFields = MandatoryFields.CreateAllPlaceholders(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a new storefront from a theme template.
    /// </summary>
    public static Storefront CreateFromTheme(Guid tenantId, string name, Guid themeId)
    {
        var storefront = CreateFromScratch(tenantId, name);
        storefront.ThemeId = themeId;
        return storefront;
    }

    /// <summary>
    /// Updates basic storefront information.
    /// </summary>
    public void UpdateInfo(string name, string? logoUrl = null, string? contactEmail = null, 
        string? contactPhone = null, string? address = null, string? currency = null, 
        string? country = null, string? brandColor = null, string? copyright = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name;

        LogoUrl = logoUrl;
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
        Address = address;

        if (!string.IsNullOrWhiteSpace(currency))
            Currency = currency;

        Country = country;
        BrandColor = brandColor;
        Copyright = copyright;

        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the tracking of mandatory fields based on provided values.
    /// </summary>
    public void UpdateMandatoryFieldsStatus(
        bool? storeNameProvided = null,
        bool? logoProvided = null,
        bool? emailProvided = null,
        bool? phoneProvided = null,
        bool? addressProvided = null,
        bool? currencyProvided = null,
        bool? countryProvided = null,
        bool? brandColorProvided = null,
        bool? copyrightProvided = null)
    {
        var fields = MandatoryFields;

        if (storeNameProvided.HasValue)
            fields = fields.WithFieldUpdated("storename", !storeNameProvided.Value);
        if (logoProvided.HasValue)
            fields = fields.WithFieldUpdated("logo", !logoProvided.Value);
        if (emailProvided.HasValue)
            fields = fields.WithFieldUpdated("email", !emailProvided.Value);
        if (phoneProvided.HasValue)
            fields = fields.WithFieldUpdated("phone", !phoneProvided.Value);
        if (addressProvided.HasValue)
            fields = fields.WithFieldUpdated("address", !addressProvided.Value);
        if (currencyProvided.HasValue)
            fields = fields.WithFieldUpdated("currency", !currencyProvided.Value);
        if (countryProvided.HasValue)
            fields = fields.WithFieldUpdated("country", !countryProvided.Value);
        if (brandColorProvided.HasValue)
            fields = fields.WithFieldUpdated("brandcolor", !brandColorProvided.Value);
        if (copyrightProvided.HasValue)
            fields = fields.WithFieldUpdated("copyright", !copyrightProvided.Value);

        MandatoryFields = fields;
        UpdateTimestamp();
    }

    /// <summary>
    /// Publishes the storefront (makes it publicly accessible).
    /// </summary>
    public void Publish()
    {
        if (Status == StorefrontStatus.Published)
            throw new InvalidOperationException("Storefront is already published.");

        if (!MandatoryFields.AreAllFieldsProvided())
            throw new InvalidOperationException("Cannot publish storefront with incomplete mandatory fields.");

        Status = StorefrontStatus.Published;
        PublishedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    /// <summary>
    /// Unpublishes the storefront (makes it draft).
    /// </summary>
    public void Unpublish()
    {
        if (Status != StorefrontStatus.Published)
            throw new InvalidOperationException("Only published storefronts can be unpublished.");

        Status = StorefrontStatus.Draft;
        UpdateTimestamp();
    }

    /// <summary>
    /// Archives the storefront.
    /// </summary>
    public void Archive()
    {
        if (Status == StorefrontStatus.Archived)
            throw new InvalidOperationException("Storefront is already archived.");

        Status = StorefrontStatus.Archived;
        UpdateTimestamp();
    }

    /// <summary>
    /// Adds a page to the storefront.
    /// </summary>
    public void AddPage(StorefrontPage page)
    {
        if (page == null)
            throw new ArgumentNullException(nameof(page));
        if (page.TenantId != TenantId)
            throw new InvalidOperationException("Page must belong to the same tenant.");

        Pages.Add(page);
        UpdateTimestamp();
    }

    /// <summary>
    /// Removes a page from the storefront.
    /// </summary>
    public void RemovePage(Guid pageId)
    {
        var page = Pages.FirstOrDefault(p => p.Id == pageId);
        if (page != null)
        {
            Pages.Remove(page);
            UpdateTimestamp();
        }
    }

    /// <summary>
    /// Gets a page by ID.
    /// </summary>
    public StorefrontPage? GetPage(Guid pageId)
    {
        return Pages.FirstOrDefault(p => p.Id == pageId);
    }
}
