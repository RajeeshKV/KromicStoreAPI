namespace KromicStore.Infrastructure.Services;

using KromicStore.Application.Interfaces;
using KromicStore.Domain.Entities;
using KromicStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for providing storefront bootstrap data.
/// Loads theme and storefront configuration from database.
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
            .FirstOrDefaultAsync(t => t.TenantId == tenantId.ToString(), cancellationToken);

        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant not found: {tenantId}");
        }

        // Fetch tenant's active theme
        var theme = await _context.Themes
            .FirstOrDefaultAsync(t => (t.OwnerTenantId == tenantId || t.OwnerTenantId == null) && t.IsActive, cancellationToken);

        // Fetch ONLY PUBLISHED storefront with all related data
        var storefront = await _context.Storefronts
            .Include(s => s.Pages)
                .ThenInclude(p => p.Sections)
                    .ThenInclude(s => s.Components)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == Domain.Enums.StorefrontStatus.Published, cancellationToken);

        if (storefront == null)
        {
            throw new InvalidOperationException("Storefront is not published");
        }

        // Fetch categories for navigation
        var categories = await _context.Categories
            .Where(c => c.TenantId == tenantId && c.ParentCategoryId == null)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);

        // Build response with actual database values
        var response = new StoreBootstrapResponse
        {
            Theme = theme != null ? new ThemeBootstrapData
            {
                PrimaryColor = theme.PrimaryColor ?? "#000000",
                SecondaryColor = theme.SecondaryColor ?? "#666666",
                AccentColor = "#007bff",
                BackgroundColor = "#ffffff",
                TextColor = "#333333",
                FontFamily = "Inter, sans-serif",
                BorderRadius = 8,
                SpacingUnit = 16,
                ComponentOverrides = "{}",
                LayoutOptions = "{}"
            } : null,
            Storefront = new StorefrontBootstrapData
            {
                SiteTitle = storefront.Name,
                MetaDescription = storefront.Copyright ?? $"Welcome to {tenant.Name}",
                FaviconUrl = storefront.LogoUrl ?? "",
                OpenGraphImageUrl = storefront.LogoUrl ?? "",
                HeaderMenu = BuildHeaderMenu(),
                FooterMenu = BuildFooterMenu(),
                Categories = categories.Select(c => new CategoryItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Name.ToLowerInvariant().Replace(' ', '-'),
                    DisplayOrder = c.DisplayOrder,
                    Children = new List<CategoryItem>()
                }).ToList(),
                HomepageLayoutType = storefront.Pages.FirstOrDefault(p => p.Slug == "home")?.LayoutType ?? "default",
                HomepageSections = BuildHomepageSections(storefront),
                WishlistEnabled = true,
                ReviewsEnabled = true,
                BlogEnabled = false,
                CouponsEnabled = true,
                MultiCurrencyEnabled = false,
                MultiLanguageEnabled = false
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

        // Fetch tenant data
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.TenantId == tenantId.ToString(), cancellationToken);

        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant not found: {tenantId}");
        }

        // Fetch tenant's active theme
        var theme = await _context.Themes
            .FirstOrDefaultAsync(t => (t.OwnerTenantId == tenantId || t.OwnerTenantId == null) && t.IsActive, cancellationToken);

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

        // Fetch categories for navigation
        var categories = await _context.Categories
            .Where(c => c.TenantId == tenantId && c.ParentCategoryId == null)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);

        // Build response with actual database values
        var response = new StoreBootstrapResponse
        {
            Theme = theme != null ? new ThemeBootstrapData
            {
                PrimaryColor = theme.PrimaryColor ?? "#000000",
                SecondaryColor = theme.SecondaryColor ?? "#666666",
                AccentColor = "#007bff",
                BackgroundColor = "#ffffff",
                TextColor = "#333333",
                FontFamily = "Inter, sans-serif",
                BorderRadius = 8,
                SpacingUnit = 16,
                ComponentOverrides = "{}",
                LayoutOptions = "{}"
            } : null,
            Storefront = new StorefrontBootstrapData
            {
                SiteTitle = storefront.Name,
                MetaDescription = storefront.Copyright ?? $"Welcome to {tenant.Name}",
                FaviconUrl = storefront.LogoUrl ?? "",
                OpenGraphImageUrl = storefront.LogoUrl ?? "",
                HeaderMenu = BuildHeaderMenu(),
                FooterMenu = BuildFooterMenu(),
                Categories = categories.Select(c => new CategoryItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Name.ToLowerInvariant().Replace(' ', '-'),
                    DisplayOrder = c.DisplayOrder,
                    Children = new List<CategoryItem>()
                }).ToList(),
                HomepageLayoutType = storefront.Pages.FirstOrDefault(p => p.Slug == "home")?.LayoutType ?? "default",
                HomepageSections = BuildHomepageSections(storefront),
                WishlistEnabled = true,
                ReviewsEnabled = true,
                BlogEnabled = false,
                CouponsEnabled = true,
                MultiCurrencyEnabled = false,
                MultiLanguageEnabled = false
            }
        };

        _logger.LogInformation("Preview bootstrap data fetched successfully for tenant: {TenantId}", tenantId);

        return response;
    }

    private List<NavigationItem> BuildHeaderMenu()
    {
        return new List<NavigationItem>
        {
            new NavigationItem { Label = "Home", Url = "/", OpensInNewTab = false },
            new NavigationItem { Label = "Products", Url = "/products", OpensInNewTab = false },
            new NavigationItem { Label = "About", Url = "/about", OpensInNewTab = false },
            new NavigationItem { Label = "Contact", Url = "/contact", OpensInNewTab = false }
        };
    }

    private List<NavigationItem> BuildFooterMenu()
    {
        return new List<NavigationItem>
        {
            new NavigationItem { Label = "Privacy Policy", Url = "/privacy", OpensInNewTab = false },
            new NavigationItem { Label = "Terms of Service", Url = "/terms", OpensInNewTab = false }
        };
    }

    private List<SectionData> BuildHomepageSections(Storefront? storefront)
    {
        if (storefront == null)
        {
            return new List<SectionData>();
        }

        var homePage = storefront.Pages.FirstOrDefault(p => p.Slug == "home");
        if (homePage == null)
        {
            return new List<SectionData>();
        }

        return homePage.Sections.OrderBy(s => s.DisplayOrder).Select(s => new SectionData
        {
            Type = s.Name,
            Name = s.Name,
            DisplayOrder = s.DisplayOrder,
            Config = new Dictionary<string, object>
            {
                { "isVisible", s.IsVisible },
                { "cssClass", s.CssClass ?? string.Empty },
                { "backgroundColor", s.BackgroundColor ?? string.Empty },
                { "backgroundImageUrl", s.BackgroundImageUrl ?? string.Empty }
            }
        }).ToList();
    }
}
