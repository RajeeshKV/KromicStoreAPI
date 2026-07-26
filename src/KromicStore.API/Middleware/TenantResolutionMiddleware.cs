using System.Security.Claims;
using KromicStore.API.Configuration;
using KromicStore.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace KromicStore.API.Middleware;

/// <summary>
/// Middleware for extracting and validating tenant information from JWT tokens or request context
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;
    private readonly TenantResolutionOptions _options;

    /// <summary>
    /// Initializes a new instance of TenantResolutionMiddleware
    /// </summary>
    /// <param name="next">The next middleware in the pipeline</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="options">Configuration options for TenantResolutionMiddleware</param>
    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> logger,
        IOptions<TenantResolutionOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new TenantResolutionOptions();
    }

    /// <summary>
    /// Processes the HTTP request and resolves tenant information
    /// </summary>
    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? "UNKNOWN";

        // Check if endpoint is in bypass paths
        if (IsPathInBypassList(context.Request.Path))
        {
            _logger.LogDebug(
                "Skipping tenant resolution for bypass endpoint - Path: {Path}, CorrelationId: {CorrelationId}",
                context.Request.Path,
                correlationId);
            
            await _next(context);
            return;
        }

        // Extract tenant ID from JWT token
        var tenantId = ExtractTenantIdFromToken(context);

        // Try header as fallback if allowed
        if (tenantId == Guid.Empty && _options.AllowTenantIdFromHeaders)
        {
            var headerValue = context.Request.Headers[_options.TenantIdHeaderName].ToString();
            if (Guid.TryParse(headerValue, out var headerTenantId))
            {
                tenantId = headerTenantId;
            }
        }

        if (tenantId == Guid.Empty)
        {
            _logger.LogWarning(
                "Request rejected - Missing or invalid tenant information - Path: {Path}, CorrelationId: {CorrelationId}, User: {User}",
                context.Request.Path,
                correlationId,
                context.User?.Identity?.Name ?? "ANONYMOUS");
            
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Missing or invalid tenant information",
                errorCode = "MISSING_TENANT"
            });
            return;
        }

        // Set tenant in context
        tenantProvider.SetTenant(tenantId, tenantId.ToString());

        _logger.LogInformation(
            "Tenant resolved successfully - TenantId: {TenantId}, Path: {Path}, CorrelationId: {CorrelationId}, User: {User}",
            tenantId,
            context.Request.Path,
            correlationId,
            context.User?.Identity?.Name ?? "ANONYMOUS");

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing request for tenant - TenantId: {TenantId}, Path: {Path}, CorrelationId: {CorrelationId}, ExceptionType: {ExceptionType}",
                tenantId,
                context.Request.Path,
                correlationId,
                ex.GetType().Name);
            throw;
        }
    }

    /// <summary>
    /// Extracts tenant ID from JWT token claims
    /// </summary>
    private Guid ExtractTenantIdFromToken(HttpContext context)
    {
        // Try to get tenant_id claim from authenticated user
        var tenantClaim = context.User?.FindFirst(_options.TenantIdClaimName);

        if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var tenantId))
        {
            return tenantId;
        }

        // Fallback: try to get from nameidentifier claim
        var userClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier);
        if (userClaim != null && Guid.TryParse(userClaim.Value, out var userId))
        {
            // In a real scenario, you would look up the tenant from the user ID
            // For now, we'll return empty to force authentication
        }

        return Guid.Empty;
    }

    /// <summary>
    /// Determines if the path is in the bypass list
    /// </summary>
    private bool IsPathInBypassList(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;

        if (!_options.UseWildcardMatching)
        {
            // Exact matching
            return _options.BypassPaths.Any(p => pathValue == p.ToLowerInvariant());
        }

        // Wildcard/prefix matching
        return _options.BypassPaths.Any(p =>
        {
            var bypassPath = p.ToLowerInvariant();
            
            if (bypassPath.EndsWith("*"))
            {
                // Pattern like "/swagger/*" or "/api/*"
                var prefix = bypassPath.TrimEnd('*');
                return pathValue.StartsWith(prefix);
            }
            
            if (bypassPath.EndsWith("/"))
            {
                // Pattern like "/swagger/" - match exact or with trailing path
                return pathValue == bypassPath.TrimEnd('/') || pathValue.StartsWith(bypassPath);
            }

            // Exact match
            return pathValue == bypassPath;
        });
    }
}
