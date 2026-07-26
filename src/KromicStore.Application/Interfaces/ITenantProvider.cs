namespace KromicStore.Application.Interfaces;

/// <summary>
/// Interface for resolving the current tenant context.
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// Gets the current tenant ID.
    /// </summary>
    Guid TenantId { get; }

    /// <summary>
    /// Gets the current tenant string identifier.
    /// </summary>
    string TenantIdentifier { get; }

    /// <summary>
    /// Checks if a tenant is set.
    /// </summary>
    bool IsSet { get; }

    /// <summary>
    /// Sets the current tenant context.
    /// </summary>
    void SetTenant(Guid tenantId, string tenantIdentifier);

    /// <summary>
    /// Clears the tenant context.
    /// </summary>
    void ClearTenant();
}
