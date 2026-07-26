using System.Net;
using Microsoft.EntityFrameworkCore;
using KromicStore.Infrastructure.Data;

namespace KromicStore.API.Middleware;

/// <summary>
/// Middleware for handling subdomain-based routing to tenant websites.
/// Extracts subdomain from the request and redirects to the tenant's frontend URL.
/// </summary>
public class SubdomainRoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SubdomainRoutingMiddleware> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of SubdomainRoutingMiddleware
    /// </summary>
    public SubdomainRoutingMiddleware(
        RequestDelegate next,
        ILogger<SubdomainRoutingMiddleware> logger,
        IServiceScopeFactory scopeFactory)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    /// <summary>
    /// Processes the HTTP request and handles subdomain routing
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host.Host.ToLowerInvariant();
        
        // Extract subdomain (everything before the main domain)
        // Assuming main domain is kromic.in
        var mainDomain = "kromic.in";
        
        if (!host.EndsWith(mainDomain))
        {
            // Not our domain, proceed normally
            await _next(context);
            return;
        }

        var subdomain = host.Substring(0, host.Length - mainDomain.Length - 1); // -1 to remove the dot
        
        // Remove www if present
        if (subdomain == "www" || string.IsNullOrEmpty(subdomain))
        {
            await _next(context);
            return;
        }

        // Check if this is an API subdomain (should not redirect)
        if (subdomain == "api")
        {
            await _next(context);
            return;
        }

        // Look up tenant by subdomain
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var tenant = await dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Subdomain.ToLower() == subdomain && t.IsActive);

        if (tenant == null)
        {
            // Tenant not found, return 404 or redirect to main site
            _logger.LogWarning("Tenant not found for subdomain: {Subdomain}", subdomain);
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant not found" });
            return;
        }

        // Redirect to tenant's frontend URL
        // The frontend URL should be constructed as: https://{subdomain}.kromic.in
        var tenantUrl = $"https://{subdomain}.{mainDomain}";
        
        // If the request is for login, add appropriate query parameters
        var path = context.Request.Path.Value ?? "";
        var queryString = context.Request.QueryString.Value ?? "";
        
        _logger.LogInformation("Redirecting subdomain {Subdomain} to tenant URL: {TenantUrl}", subdomain, tenantUrl);
        
        context.Response.Redirect($"{tenantUrl}{path}{queryString}");
    }
}
