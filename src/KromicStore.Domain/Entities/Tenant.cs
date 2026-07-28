namespace KromicStore.Domain.Entities;

/// <summary>
/// Represents a tenant in the multi-tenant system.
/// </summary>
public class Tenant : BaseEntity
{
    /// <summary>Gets the unique identifier for the tenant.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the tenant name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the subdomain for the tenant (e.g., "mystore" for mystore.kromic.in). This is the simple subdomain setup without DNS verification.</summary>
    public string Subdomain { get; private set; } = string.Empty;

    /// <summary>Gets the tenant description.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Gets the tenant logo URL.</summary>
    public string? LogoUrl { get; private set; }

    /// <summary>Gets a value indicating whether the tenant is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets a value indicating whether the tenant is archived.</summary>
    public bool IsArchived { get; private set; }

    /// <summary>Gets a value indicating whether the tenant has been soft deleted.</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>Gets the date when the tenant was suspended.</summary>
    public DateTime? SuspendedAt { get; private set; }

    /// <summary>Gets the date when the tenant was archived.</summary>
    public DateTime? ArchivedAt { get; private set; }

    /// <summary>Gets the date when the tenant was soft deleted.</summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>Gets the latest lifecycle reason.</summary>
    public string? LifecycleReason { get; private set; }

    /// <summary>Gets the subscription plan identifier.</summary>
    public string SubscriptionPlan { get; private set; } = "basic";

    /// <summary>Gets the contact email.</summary>
    public string ContactEmail { get; private set; } = string.Empty;

    /// <summary>Gets the contact phone number.</summary>
    public string? ContactPhone { get; private set; }

    /// <summary>Gets the subscription end date.</summary>
    public DateTime? SubscriptionEndDate { get; private set; }

    /// <summary>
    /// Creates a new instance of Tenant.
    /// </summary>
    public static Tenant Create(string tenantId, string name, string subdomain, string description, string contactEmail)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(subdomain))
            throw new ArgumentException("Subdomain is required.", nameof(subdomain));
        if (string.IsNullOrWhiteSpace(contactEmail))
            throw new ArgumentException("Contact email is required.", nameof(contactEmail));

        // Validate subdomain format (alphanumeric, hyphens only, no spaces)
        if (!IsValidSubdomain(subdomain))
            throw new ArgumentException("Subdomain must contain only alphanumeric characters and hyphens.", nameof(subdomain));

        return new Tenant
        {
            TenantId = tenantId,
            Name = name,
            Subdomain = subdomain.ToLowerInvariant(),
            Description = description,
            ContactEmail = contactEmail,
            IsActive = true,
            SubscriptionPlan = "basic"
        };
    }

    /// <summary>
    /// Validates subdomain format.
    /// </summary>
    private static bool IsValidSubdomain(string subdomain)
    {
        // Only allow alphanumeric and hyphens, must start and end with alphanumeric
        return System.Text.RegularExpressions.Regex.IsMatch(subdomain, @"^[a-z0-9]([a-z0-9-]*[a-z0-9])?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Updates tenant information.
    /// </summary>
    public void Update(string name, string description, string contactEmail, string? contactPhone = null)
    {
        Name = name;
        Description = description;
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
    }

    /// <summary>
    /// Deactivates the tenant.
    /// </summary>
    public void Deactivate()
    {
        Suspend("Deactivated");
    }

    /// <summary>
    /// Activates the tenant.
    /// </summary>
    public void Activate()
    {
        Restore("Activated");
    }

    /// <summary>
    /// Suspends the tenant without deleting data.
    /// </summary>
    public void Suspend(string? reason = null)
    {
        IsActive = false;
        SuspendedAt = DateTime.UtcNow;
        LifecycleReason = reason;
        UpdateTimestamp();
    }

    /// <summary>
    /// Archives the tenant for long-term retention.
    /// </summary>
    public void Archive(string? reason = null)
    {
        IsActive = false;
        IsArchived = true;
        ArchivedAt = DateTime.UtcNow;
        LifecycleReason = reason;
        UpdateTimestamp();
    }

    /// <summary>
    /// Soft deletes the tenant while preserving records for restore/retention.
    /// </summary>
    public void SoftDelete(string? reason = null)
    {
        IsActive = false;
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        LifecycleReason = reason;
        UpdateTimestamp();
    }

    /// <summary>
    /// Restores a suspended, archived, or soft-deleted tenant.
    /// </summary>
    public void Restore(string? reason = null)
    {
        IsActive = true;
        IsArchived = false;
        IsDeleted = false;
        SuspendedAt = null;
        ArchivedAt = null;
        DeletedAt = null;
        LifecycleReason = reason;
        UpdateTimestamp();
    }
}
