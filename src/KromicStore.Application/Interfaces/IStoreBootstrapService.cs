namespace KromicStore.Application.Interfaces;

/// <summary>
/// Service for providing storefront bootstrap data.
/// </summary>
public interface IStoreBootstrapService
{
    /// <summary>
    /// Gets the complete bootstrap data for the current tenant.
    /// </summary>
    Task<StoreBootstrapResponse> GetBootstrapDataAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Response containing all data required to bootstrap the storefront.
/// </summary>
public class StoreBootstrapResponse
{
    public TenantBootstrapData? Tenant { get; set; }
    public ThemeBootstrapData? Theme { get; set; }
    public NavigationBootstrapData? Navigation { get; set; }
    public HomepageBootstrapData? Homepage { get; set; }
    public FeaturesBootstrapData? Features { get; set; }
    public SeoBootstrapData? Seo { get; set; }
}

/// <summary>
/// Tenant bootstrap data.
/// </summary>
public class TenantBootstrapData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
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
/// Navigation bootstrap data.
/// </summary>
public class NavigationBootstrapData
{
    public List<NavigationItem> HeaderMenu { get; set; } = new();
    public List<NavigationItem> FooterMenu { get; set; } = new();
    public List<CategoryItem> Categories { get; set; } = new();
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
/// Homepage bootstrap data.
/// </summary>
public class HomepageBootstrapData
{
    public string LayoutType { get; set; } = string.Empty;
    public List<SectionData> Sections { get; set; } = new();
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

/// <summary>
/// Features bootstrap data (feature flags).
/// </summary>
public class FeaturesBootstrapData
{
    public bool WishlistEnabled { get; set; }
    public bool ReviewsEnabled { get; set; }
    public bool BlogEnabled { get; set; }
    public bool CouponsEnabled { get; set; }
    public bool MultiCurrencyEnabled { get; set; }
    public bool MultiLanguageEnabled { get; set; }
}

/// <summary>
/// SEO bootstrap data.
/// </summary>
public class SeoBootstrapData
{
    public string SiteTitle { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public string FaviconUrl { get; set; } = string.Empty;
    public string OpenGraphImageUrl { get; set; } = string.Empty;
}
