namespace KromicStore.Domain.Entities;

/// <summary>
/// Represents a domain associated with a tenant (supports multiple domains per tenant).
/// </summary>
public class TenantDomain : BaseEntity
{
    /// <summary>Gets the tenant ID this domain belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the domain name (e.g., "store.example.com").</summary>
    public string Domain { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether this is the primary domain for the tenant.</summary>
    public bool IsPrimary { get; private set; }

    /// <summary>Gets a value indicating whether the domain ownership has been verified.</summary>
    public bool IsVerified { get; private set; }

    /// <summary>Navigation property to the tenant.</summary>
    public Tenant? Tenant { get; private set; }

    /// <summary>
    /// Creates a new instance of TenantDomain.
    /// </summary>
    public static TenantDomain Create(Guid tenantId, string domain, bool isPrimary = false)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(domain))
            throw new ArgumentException("Domain is required.", nameof(domain));

        // Normalize domain to lowercase
        var normalizedDomain = domain.ToLowerInvariant().Trim().TrimEnd('.');

        return new TenantDomain
        {
            TenantId = tenantId,
            Domain = normalizedDomain,
            IsPrimary = isPrimary,
            IsVerified = false // New domains require verification
        };
    }

    /// <summary>
    /// Marks the domain as verified.
    /// </summary>
    public void MarkAsVerified()
    {
        IsVerified = true;
    }

    /// <summary>
    /// Marks the domain as unverified.
    /// </summary>
    public void MarkAsUnverified()
    {
        IsVerified = false;
    }

    /// <summary>
    /// Sets this domain as the primary domain for the tenant.
    /// </summary>
    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    /// <summary>
    /// Sets this domain as a non-primary domain.
    /// </summary>
    public void SetAsNonPrimary()
    {
        IsPrimary = false;
    }

    /// <summary>
    /// Updates the domain name.
    /// </summary>
    public void UpdateDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            throw new ArgumentException("Domain is required.", nameof(domain));

        Domain = domain.ToLowerInvariant().Trim().TrimEnd('.');
    }
}
