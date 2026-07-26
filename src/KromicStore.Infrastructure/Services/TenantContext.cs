namespace KromicStore.Infrastructure.Services;

using KromicStore.Application.Interfaces;

/// <summary>
/// Implementation of tenant context that stores tenant information for the current request.
/// </summary>
public class TenantContext : ITenantContext
{
    private Guid _tenantId = Guid.Empty;
    private string _tenantName = string.Empty;
    private string _slug = string.Empty;
    private string _domain = string.Empty;
    private string _locale = "en-US";
    private string _currency = "USD";
    private string _timezone = "UTC";

    /// <inheritdoc />
    public Guid TenantId => _tenantId;

    /// <inheritdoc />
    public string TenantName => _tenantName;

    /// <inheritdoc />
    public string Slug => _slug;

    /// <inheritdoc />
    public string Domain => _domain;

    /// <inheritdoc />
    public string Locale => _locale;

    /// <inheritdoc />
    public string Currency => _currency;

    /// <inheritdoc />
    public string Timezone => _timezone;

    /// <inheritdoc />
    public bool IsResolved => _tenantId != Guid.Empty;

    /// <inheritdoc />
    public void SetContext(Guid tenantId, string tenantName, string slug, string domain, string locale, string currency, string timezone)
    {
        _tenantId = tenantId;
        _tenantName = tenantName;
        _slug = slug;
        _domain = domain;
        _locale = locale;
        _currency = currency;
        _timezone = timezone;
    }

    /// <inheritdoc />
    public void Clear()
    {
        _tenantId = Guid.Empty;
        _tenantName = string.Empty;
        _slug = string.Empty;
        _domain = string.Empty;
        _locale = "en-US";
        _currency = "USD";
        _timezone = "UTC";
    }
}
