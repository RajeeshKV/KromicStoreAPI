namespace KromicStore.Application.Interfaces;

/// <summary>
/// Service for providing storefront bootstrap data.
/// </summary>
public interface IStoreBootstrapService
{
    /// <summary>
    /// Gets the complete bootstrap data for the current tenant.
    /// Only returns published storefront data for public access.
    /// </summary>
    Task<StoreBootstrapResponse> GetBootstrapDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the complete bootstrap data for storefront preview.
    /// Returns draft state including unpublished changes for tenant admin preview.
    /// </summary>
    Task<StoreBootstrapResponse> GetPreviewDataAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Response containing theme and storefront configuration data.
/// </summary>
public class StoreBootstrapResponse
{
    /// <summary>Theme configuration data.</summary>
    public ThemeBootstrapData? Theme { get; set; }

    /// <summary>Storefront configuration data (navigation, homepage, features, SEO).</summary>
    public StorefrontBootstrapData? Storefront { get; set; }
}

/// <summary>
/// Storefront bootstrap data (consolidated view).
/// </summary>
public class StorefrontBootstrapData
{
    public string SiteTitle { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public string FaviconUrl { get; set; } = string.Empty;
    public string OpenGraphImageUrl { get; set; } = string.Empty;
    public List<NavigationItem> HeaderMenu { get; set; } = new();
    public List<NavigationItem> FooterMenu { get; set; } = new();
    public List<CategoryItem> Categories { get; set; } = new();
    public string HomepageLayoutType { get; set; } = string.Empty;
    public List<SectionData> HomepageSections { get; set; } = new();
    public bool WishlistEnabled { get; set; }
    public bool ReviewsEnabled { get; set; }
    public bool BlogEnabled { get; set; }
    public bool CouponsEnabled { get; set; }
    public bool MultiCurrencyEnabled { get; set; }
    public bool MultiLanguageEnabled { get; set; }
}

/// <summary>
/// Theme bootstrap data.
/// </summary>
public class ThemeBootstrapData
{
    public string PrimaryColor { get; set; } = string.Empty;
    public string SecondaryColor { get; set; } = string.Empty;
    public string AccentColor { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public string FontFamily { get; set; } = string.Empty;
    public int BorderRadius { get; set; }
    public int SpacingUnit { get; set; }
    public string ComponentOverrides { get; set; } = string.Empty;
    public string LayoutOptions { get; set; } = string.Empty;
}

/// <summary>
/// Navigation item.
/// </summary>
public class NavigationItem
{
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool OpensInNewTab { get; set; }
}

/// <summary>
/// Category item.
/// </summary>
public class CategoryItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public List<CategoryItem> Children { get; set; } = new();
}

/// <summary>
/// Section data for homepage.
/// </summary>
public class SectionData
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public Dictionary<string, object> Config { get; set; } = new();
}
