namespace KromicStore.Application.Interfaces;

/// <summary>
/// Provides access to the current tenant context resolved from the request.
/// </summary>
public interface ITenantContext
{
    /// <summary>Gets the tenant ID.</summary>
    Guid TenantId { get; }

    /// <summary>Gets the tenant name.</summary>
    string TenantName { get; }

    /// <summary>Gets the tenant slug/subdomain.</summary>
    string Slug { get; }

    /// <summary>Gets the domain that was used to resolve the tenant.</summary>
    string Domain { get; }

    /// <summary>Gets the tenant's locale.</summary>
    string Locale { get; }

    /// <summary>Gets the tenant's currency.</summary>
    string Currency { get; }

    /// <summary>Gets the tenant's timezone.</summary>
    string Timezone { get; }

    /// <summary>Gets a value indicating whether the tenant context has been resolved.</summary>
    bool IsResolved { get; }

    /// <summary>
    /// Sets the tenant context information.
    /// </summary>
    void SetContext(Guid tenantId, string tenantName, string slug, string domain, string locale, string currency, string timezone);

    /// <summary>
    /// Clears the tenant context.
    /// </summary>
    void Clear();
}
