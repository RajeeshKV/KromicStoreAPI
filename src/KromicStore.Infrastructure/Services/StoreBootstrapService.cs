namespace KromicStore.Infrastructure.Services;

using KromicStore.Application.Interfaces;
using KromicStore.Domain.Entities;
using KromicStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for providing storefront bootstrap data.
/// </summary>
public class StoreBootstrapService : IStoreBootstrapService
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<StoreBootstrapService> _logger;

    public StoreBootstrapService(
        AppDbContext context,
        ITenantContext tenantContext,
        ILogger<StoreBootstrapService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<StoreBootstrapResponse> GetBootstrapDataAsync(CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.IsResolved)
        {
            throw new InvalidOperationException("Tenant context is not resolved");
        }

        var tenantId = _tenantContext.TenantId;

        _logger.LogInformation("Fetching public bootstrap data for tenant: {TenantId}", tenantId);

        // Fetch tenant data
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant not found: {tenantId}");
        }

        // Fetch tenant theme (from unified Theme table - tenant or platform themes)
        var theme = await _context.Themes
            .FirstOrDefaultAsync(t => (t.OwnerTenantId == tenantId || t.OwnerTenantId == null) && t.IsActive, cancellationToken);

        // Fetch categories for navigation
        var categories = await _context.Categories
            .Where(c => c.TenantId == tenantId && c.ParentCategoryId == null)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);

        // Fetch ONLY PUBLISHED storefront for homepage
        var storefront = await _context.Storefronts
            .Include(s => s.Pages)
                .ThenInclude(p => p.Sections)
                    .ThenInclude(s => s.Components)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == Domain.Enums.StorefrontStatus.Published, cancellationToken);

        if (storefront == null)
        {
            throw new InvalidOperationException("Storefront is not published");
        }

        // Build response
        var response = new StoreBootstrapResponse
        {
            Tenant = new TenantBootstrapData
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Slug = tenant.Subdomain,
                LogoUrl = "", // TODO: Load from tenant settings
                Status = tenant.IsActive ? "active" : "inactive",
                Locale = _tenantContext.Locale,
                Currency = _tenantContext.Currency,
                Timezone = _tenantContext.Timezone
            },
            Theme = theme != null ? new ThemeBootstrapData
            {
                // NOTE: Using legacy color fields for backward compatibility
                // These will be phased out in favor of parsing DefinitionJson
                PrimaryColor = theme.PrimaryColor ?? "#000000",
                SecondaryColor = theme.SecondaryColor ?? "#666666",
                AccentColor = "#007bff", // Default
                BackgroundColor = "#ffffff",
                TextColor = "#333333",
                FontFamily = "Inter, sans-serif",
                BorderRadius = 8,
                SpacingUnit = 16,
                ComponentOverrides = "{}",
                LayoutOptions = "{}"
            } : null,
            Navigation = new NavigationBootstrapData
            {
                HeaderMenu = new List<NavigationItem>
                {
                    new NavigationItem { Label = "Home", Url = "/", OpensInNewTab = false },
                    new NavigationItem { Label = "Products", Url = "/products", OpensInNewTab = false },
                    new NavigationItem { Label = "About", Url = "/about", OpensInNewTab = false },
                    new NavigationItem { Label = "Contact", Url = "/contact", OpensInNewTab = false }
                },
                FooterMenu = new List<NavigationItem>
                {
                    new NavigationItem { Label = "Privacy Policy", Url = "/privacy", OpensInNewTab = false },
                    new NavigationItem { Label = "Terms of Service", Url = "/terms", OpensInNewTab = false }
                },
                Categories = categories.Select(c => new CategoryItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Name.ToLowerInvariant().Replace(' ', '-'), // Generate slug from name
                    DisplayOrder = c.DisplayOrder,
                    Children = new List<CategoryItem>() // TODO: Load children recursively
                }).ToList()
            },
            Homepage = BuildHomepageData(storefront),
            Features = new FeaturesBootstrapData
            {
                WishlistEnabled = true, // TODO: Load from tenant settings
                ReviewsEnabled = true,
                BlogEnabled = false,
                CouponsEnabled = true,
                MultiCurrencyEnabled = false,
                MultiLanguageEnabled = false
            },
            Seo = new SeoBootstrapData
            {
                SiteTitle = $"{tenant.Name} Store",
                MetaDescription = $"Welcome to {tenant.Name} online store",
                FaviconUrl = "", // TODO: Load from tenant settings
                OpenGraphImageUrl = "" // TODO: Load from tenant settings
            }
        };

        _logger.LogInformation("Public bootstrap data fetched successfully for tenant: {TenantId}", tenantId);

        return response;
    }

    /// <inheritdoc />
    public async Task<StoreBootstrapResponse> GetPreviewDataAsync(CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.IsResolved)
        {
            throw new InvalidOperationException("Tenant context is not resolved");
        }

        var tenantId = _tenantContext.TenantId;

        _logger.LogInformation("Fetching preview bootstrap data for tenant: {TenantId}", tenantId);

        // Fetch tenant data - match by TenantId (string) not Id (GUID)
        // The JWT contains the string TenantId, not the GUID primary key
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.TenantId == tenantId.ToString(), cancellationToken);

        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant not found: {tenantId}");
        }

        // Fetch tenant theme (from unified Theme table - tenant or platform themes)
        var theme = await _context.Themes
            .FirstOrDefaultAsync(t => (t.OwnerTenantId == tenantId || t.OwnerTenantId == null) && t.IsActive, cancellationToken);

        // Fetch categories for navigation
        var categories = await _context.Categories
            .Where(c => c.TenantId == tenantId && c.ParentCategoryId == null)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);

        // Fetch ANY storefront (draft or published) for preview
        var storefront = await _context.Storefronts
            .Include(s => s.Pages)
                .ThenInclude(p => p.Sections)
                    .ThenInclude(s => s.Components)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);

        if (storefront == null)
        {
            throw new InvalidOperationException("Storefront not configured");
        }

        // Build response with preview metadata
        var response = new StoreBootstrapResponse
        {
            Tenant = new TenantBootstrapData
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Slug = tenant.Subdomain,
                LogoUrl = "", // TODO: Load from tenant settings
                Status = tenant.IsActive ? "active" : "inactive",
                Locale = _tenantContext.Locale,
                Currency = _tenantContext.Currency,
                Timezone = _tenantContext.Timezone
            },
            Theme = theme != null ? new ThemeBootstrapData
            {
                // NOTE: Using legacy color fields for backward compatibility
                // These will be phased out in favor of parsing DefinitionJson
                PrimaryColor = theme.PrimaryColor ?? "#000000",
                SecondaryColor = theme.SecondaryColor ?? "#666666",
                AccentColor = "#007bff", // Default
                BackgroundColor = "#ffffff",
                TextColor = "#333333",
                FontFamily = "Inter, sans-serif",
                BorderRadius = 8,
                SpacingUnit = 16,
                ComponentOverrides = "{}",
                LayoutOptions = "{}"
            } : null,
            Navigation = new NavigationBootstrapData
            {
                HeaderMenu = new List<NavigationItem>
                {
                    new NavigationItem { Label = "Home", Url = "/", OpensInNewTab = false },
                    new NavigationItem { Label = "Products", Url = "/products", OpensInNewTab = false },
                    new NavigationItem { Label = "About", Url = "/about", OpensInNewTab = false },
                    new NavigationItem { Label = "Contact", Url = "/contact", OpensInNewTab = false }
                },
                FooterMenu = new List<NavigationItem>
                {
                    new NavigationItem { Label = "Privacy Policy", Url = "/privacy", OpensInNewTab = false },
                    new NavigationItem { Label = "Terms of Service", Url = "/terms", OpensInNewTab = false }
                },
                Categories = categories.Select(c => new CategoryItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Name.ToLowerInvariant().Replace(' ', '-'),
                    DisplayOrder = c.DisplayOrder,
                    Children = new List<CategoryItem>()
                }).ToList()
            },
            Homepage = BuildHomepageData(storefront),
            Features = new FeaturesBootstrapData
            {
                WishlistEnabled = true,
                ReviewsEnabled = true,
                BlogEnabled = false,
                CouponsEnabled = true,
                MultiCurrencyEnabled = false,
                MultiLanguageEnabled = false
            },
            Seo = new SeoBootstrapData
            {
                SiteTitle = $"{tenant.Name} Store (Preview)",
                MetaDescription = $"Preview of {tenant.Name} online store",
                FaviconUrl = "",
                OpenGraphImageUrl = ""
            }
        };

        _logger.LogInformation("Preview bootstrap data fetched successfully for tenant: {TenantId}", tenantId);

        return response;
    }

    private HomepageBootstrapData? BuildHomepageData(Storefront? storefront)
    {
        if (storefront == null)
        {
            return null;
        }

        var homePage = storefront.Pages.FirstOrDefault(p => p.Slug == "home");
        if (homePage == null)
        {
            return null;
        }

        return new HomepageBootstrapData
        {
            LayoutType = homePage.LayoutType,
            Sections = homePage.Sections.OrderBy(s => s.DisplayOrder).Select(s => new SectionData
            {
                Type = s.Name, // Use Name as the section type
                Name = s.Name,
                DisplayOrder = s.DisplayOrder,
                Config = new Dictionary<string, object>
                {
                    { "isVisible", s.IsVisible },
                    { "cssClass", s.CssClass ?? string.Empty },
                    { "backgroundColor", s.BackgroundColor ?? string.Empty },
                    { "backgroundImageUrl", s.BackgroundImageUrl ?? string.Empty }
                }
            }).ToList()
        };
    }
}
