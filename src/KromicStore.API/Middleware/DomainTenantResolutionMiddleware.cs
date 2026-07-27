using KromicStore.Application.Interfaces;
using KromicStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KromicStore.API.Middleware;

/// <summary>
/// Middleware for resolving tenant from request hostname (domain-based resolution).
/// Supports both kromic.in subdomains and custom domains.
/// </summary>
public class DomainTenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DomainTenantResolutionMiddleware> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of DomainTenantResolutionMiddleware
    /// </summary>
    public DomainTenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<DomainTenantResolutionMiddleware> logger,
        IServiceScopeFactory scopeFactory)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    /// <summary>
    /// Processes the HTTP request and resolves tenant from hostname
    /// </summary>
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? "UNKNOWN";
        var host = context.Request.Host.Host.ToLowerInvariant();
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Skip tenant resolution for API endpoints and public endpoints
        if (ShouldSkipTenantResolution(path))
        {
            _logger.LogDebug(
                "Skipping domain-based tenant resolution for API endpoint - Path: {Path}, CorrelationId: {CorrelationId}",
                path,
                correlationId);
            
            await _next(context);
            return;
        }

        // Normalize hostname
        var normalizedHost = NormalizeHostname(host);

        _logger.LogInformation(
            "Resolving tenant from hostname - Host: {Host}, Normalized: {NormalizedHost}, Path: {Path}, CorrelationId: {CorrelationId}",
            host,
            normalizedHost,
            path,
            correlationId);

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Look up tenant by domain
        var tenantDomain = await dbContext.TenantDomains
            .Include(td => td.Tenant)
            .FirstOrDefaultAsync(td => td.Domain.ToLower() == normalizedHost && td.IsVerified);

        if (tenantDomain == null)
        {
            _logger.LogWarning(
                "Tenant not found for domain - Domain: {Domain}, CorrelationId: {CorrelationId}",
                normalizedHost,
                correlationId);

            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant not found" });
            return;
        }

        var tenant = tenantDomain.Tenant;
        if (tenant == null)
        {
            _logger.LogWarning(
                "Tenant domain found but tenant is null - Domain: {Domain}, TenantId: {TenantId}, CorrelationId: {CorrelationId}",
                normalizedHost,
                tenantDomain.TenantId,
                correlationId);

            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant not found" });
            return;
        }

        // Validate tenant status
        if (tenant.IsDeleted || tenant.IsArchived || !tenant.IsActive)
        {
            _logger.LogWarning(
                "Tenant is not available - TenantId: {TenantId}, TenantName: {TenantName}, Status: {Status}, CorrelationId: {CorrelationId}",
                tenant.Id,
                tenant.Name,
                tenant.IsDeleted ? "Deleted" : tenant.IsArchived ? "Archived" : "Suspended",
                correlationId);

            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant is not available" });
            return;
        }

        // Populate tenant context
        tenantContext.SetContext(
            tenantId: tenant.Id,
            tenantName: tenant.Name,
            slug: tenant.Subdomain,
            domain: normalizedHost,
            locale: "en-US", // TODO: Load from tenant settings
            currency: "INR", // TODO: Load from tenant settings
            timezone: "Asia/Kolkata" // TODO: Load from tenant settings
        );

        _logger.LogInformation(
            "Tenant resolved successfully - TenantId: {TenantId}, TenantName: {TenantName}, Domain: {Domain}, Path: {Path}, CorrelationId: {CorrelationId}",
            tenant.Id,
            tenant.Name,
            normalizedHost,
            path,
            correlationId);

        await _next(context);
    }

    /// <summary>
    /// Normalizes hostname by trimming whitespace, trailing periods, and converting to lowercase
    /// </summary>
    private string NormalizeHostname(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return string.Empty;

        return host.Trim().TrimEnd('.').ToLowerInvariant();
    }

    /// <summary>
    /// Determines if tenant resolution should be skipped for the current path
    /// </summary>
    private bool ShouldSkipTenantResolution(string path)
    {
        // Skip ALL API endpoints (including SuperUser auth and public endpoints)
        if (path.StartsWith("/api"))
            return true;

        // Skip health checks
        if (path.StartsWith("/health"))
            return true;

        // Skip Swagger/OpenAPI
        if (path.StartsWith("/swagger"))
            return true;

        // Skip Hangfire dashboard
        if (path.StartsWith("/hangfire"))
            return true;

        return false;
    }
}
