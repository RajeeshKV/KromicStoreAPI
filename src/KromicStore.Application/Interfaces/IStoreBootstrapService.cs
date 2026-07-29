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
/// Storefront bootstrap data - direct mapping from Storefront entity.
/// </summary>
public class StorefrontBootstrapData
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? ThemeId { get; set; }
    public string? LogoUrl { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string Currency { get; set; } = "INR";
    public string? Country { get; set; }
    public string? BrandColor { get; set; }
    public string? Copyright { get; set; }
    public string? FacebookUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? LinkedInUrl { get; set; }
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
