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

        // Fetch ONLY PUBLISHED storefront
        var storefront = await _context.Storefronts
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == Domain.Enums.StorefrontStatus.Published, cancellationToken);

        if (storefront == null)
        {
            throw new InvalidOperationException("Storefront is not published");
        }

        // Build response with clean Storefront entity fields only
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
                Name = storefront.Name,
                Status = storefront.Status.ToString(),
                ThemeId = storefront.ThemeId,
                LogoUrl = storefront.LogoUrl,
                ContactEmail = storefront.ContactEmail,
                ContactPhone = storefront.ContactPhone,
                Address = storefront.Address,
                Currency = storefront.Currency,
                Country = storefront.Country,
                BrandColor = storefront.BrandColor,
                Copyright = storefront.Copyright,
                FacebookUrl = storefront.FacebookUrl,
                TwitterUrl = storefront.TwitterUrl,
                InstagramUrl = storefront.InstagramUrl,
                LinkedInUrl = storefront.LinkedInUrl
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
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);

        if (storefront == null)
        {
            throw new InvalidOperationException("Storefront not configured");
        }

        // Build response with clean Storefront entity fields only
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
                Name = storefront.Name,
                Status = storefront.Status.ToString(),
                ThemeId = storefront.ThemeId,
                LogoUrl = storefront.LogoUrl,
                ContactEmail = storefront.ContactEmail,
                ContactPhone = storefront.ContactPhone,
                Address = storefront.Address,
                Currency = storefront.Currency,
                Country = storefront.Country,
                BrandColor = storefront.BrandColor,
                Copyright = storefront.Copyright,
                FacebookUrl = storefront.FacebookUrl,
                TwitterUrl = storefront.TwitterUrl,
                InstagramUrl = storefront.InstagramUrl,
                LinkedInUrl = storefront.LinkedInUrl
            }
        };

        _logger.LogInformation("Preview bootstrap data fetched successfully for tenant: {TenantId}", tenantId);

        return response;
    }
}
