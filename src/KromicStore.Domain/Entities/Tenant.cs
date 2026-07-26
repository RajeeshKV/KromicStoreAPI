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

    /// <summary>Gets the tenant description.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Gets the tenant logo URL.</summary>
    public string? LogoUrl { get; private set; }

    /// <summary>Gets a value indicating whether the tenant is active.</summary>
    public bool IsActive { get; private set; }

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
    public static Tenant Create(string tenantId, string name, string description, string contactEmail)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(contactEmail))
            throw new ArgumentException("Contact email is required.", nameof(contactEmail));

        return new Tenant
        {
            TenantId = tenantId,
            Name = name,
            Description = description,
            ContactEmail = contactEmail,
            IsActive = true,
            SubscriptionPlan = "basic"
        };
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
        IsActive = false;
    }

    /// <summary>
    /// Activates the tenant.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }
}
