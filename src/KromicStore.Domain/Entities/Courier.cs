namespace KromicStore.Domain.Entities;

/// <summary>
/// Represents a courier service configuration for a tenant.
/// Tenants can configure their preferred courier services for order delivery.
/// </summary>
public class Courier : BaseEntity
{
    /// <summary>Gets the tenant ID this courier belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the courier name (e.g., "Delhivery", "BlueDart", "FedEx").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the courier description or notes.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the tracking URL template (e.g., "https://delhivery.com/track/{tracking_id}").</summary>
    public string? TrackingUrlTemplate { get; private set; }

    /// <summary>Gets a value indicating whether this courier is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets the contact phone number for the courier.</summary>
    public string? ContactPhone { get; private set; }

    /// <summary>Gets the contact email for the courier.</summary>
    public string? ContactEmail { get; private set; }

    /// <summary>Gets the average delivery time in days.</summary>
    public int? AverageDeliveryDays { get; private set; }

    /// <summary>Navigation property to the tenant.</summary>
    public Tenant? Tenant { get; private set; }

    /// <summary>
    /// Creates a new instance of Courier.
    /// </summary>
    public static Courier Create(Guid tenantId, string name, string? description = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Courier name is required.", nameof(name));

        return new Courier
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            Description = description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates the courier information.
    /// </summary>
    public void UpdateInfo(string name, string? description = null, string? trackingUrlTemplate = null,
        string? contactPhone = null, string? contactEmail = null, int? averageDeliveryDays = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name.Trim();

        Description = description?.Trim();
        TrackingUrlTemplate = trackingUrlTemplate?.Trim();
        ContactPhone = contactPhone?.Trim();
        ContactEmail = contactEmail?.Trim();
        AverageDeliveryDays = averageDeliveryDays;

        UpdateTimestamp();
    }

    /// <summary>
    /// Activates this courier.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdateTimestamp();
    }

    /// <summary>
    /// Deactivates this courier.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }

    /// <summary>
    /// Generates the tracking URL for a given tracking ID.
    /// </summary>
    public string? GenerateTrackingUrl(string trackingId)
    {
        if (string.IsNullOrWhiteSpace(TrackingUrlTemplate) || string.IsNullOrWhiteSpace(trackingId))
            return null;

        return TrackingUrlTemplate.Replace("{tracking_id}", trackingId);
    }
}
