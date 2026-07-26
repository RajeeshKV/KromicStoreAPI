namespace KromicStore.Infrastructure.Services;

using Application.Interfaces;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Tenant provider implementation for multi-tenancy context.
/// </summary>
public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid _tenantId;
    private string _tenantIdentifier = string.Empty;

    /// <summary>
    /// Initializes a new instance of the TenantProvider class.
    /// </summary>
    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        ResolveTenantFromContext();
    }

    /// <inheritdoc />
    public Guid TenantId => _tenantId;

    /// <inheritdoc />
    public string TenantIdentifier => _tenantIdentifier;

    /// <inheritdoc />
    public bool IsSet => _tenantId != Guid.Empty && !string.IsNullOrEmpty(_tenantIdentifier);

    /// <inheritdoc />
    public void SetTenant(Guid tenantId, string tenantIdentifier)
    {
        _tenantId = tenantId;
        _tenantIdentifier = tenantIdentifier;
    }

    /// <inheritdoc />
    public void ClearTenant()
    {
        _tenantId = Guid.Empty;
        _tenantIdentifier = string.Empty;
    }

    private void ResolveTenantFromContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return;

        // Try to resolve tenant from header
        if (httpContext.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdHeader) &&
            Guid.TryParse(tenantIdHeader.ToString(), out var tenantId))
        {
            _tenantId = tenantId;
        }

        // Try to resolve tenant from subdomain or host
        var host = httpContext.Request.Host.Host;
        var parts = host.Split('.');
        if (parts.Length > 1 && parts[0] != "www")
        {
            _tenantIdentifier = parts[0];
        }
    }
}
