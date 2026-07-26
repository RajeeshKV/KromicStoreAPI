namespace KromicStore.Domain.Entities;

using Enums;
using ValueObjects;

/// <summary>
/// Represents a component within a storefront section.
/// </summary>
public class StorefrontComponent : BaseEntity
{
    /// <summary>Gets the tenant ID this component belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the parent section ID.</summary>
    public Guid SectionId { get; private set; }

    /// <summary>Gets the parent section.</summary>
    public StorefrontSection? Section { get; private set; }

    /// <summary>Gets the component type.</summary>
    public ComponentType Type { get; private set; }

    /// <summary>Gets the component configuration.</summary>
    public ComponentConfig Config { get; private set; } = null!;

    /// <summary>Gets a value indicating whether the component is visible.</summary>
    public bool IsVisible { get; private set; } = true;

    /// <summary>Gets the display order within the section.</summary>
    public int DisplayOrder { get; private set; }

    /// <summary>Gets an optional CSS class for styling.</summary>
    public string? CssClass { get; private set; }

    /// <summary>Gets an optional identifier for tracking/analytics.</summary>
    public string? TrackingId { get; private set; }

    /// <summary>
    /// Creates a new instance of StorefrontComponent.
    /// </summary>
    public static StorefrontComponent Create(
        Guid tenantId,
        Guid sectionId,
        ComponentType type,
        ComponentConfig config,
        int displayOrder = 0)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (sectionId == Guid.Empty)
            throw new ArgumentException("Section ID is required.", nameof(sectionId));

        return new StorefrontComponent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SectionId = sectionId,
            Type = type,
            Config = config,
            DisplayOrder = displayOrder,
            IsVisible = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates the component configuration.
    /// </summary>
    public void UpdateConfig(ComponentConfig config)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the display order.
    /// </summary>
    public void SetDisplayOrder(int order)
    {
        if (order < 0)
            throw new ArgumentException("Display order cannot be negative.", nameof(order));

        DisplayOrder = order;
        UpdateTimestamp();
    }

    /// <summary>
    /// Shows the component.
    /// </summary>
    public void Show()
    {
        IsVisible = true;
        UpdateTimestamp();
    }

    /// <summary>
    /// Hides the component.
    /// </summary>
    public void Hide()
    {
        IsVisible = false;
        UpdateTimestamp();
    }

    /// <summary>
    /// Toggles visibility of the component.
    /// </summary>
    public void ToggleVisibility()
    {
        IsVisible = !IsVisible;
        UpdateTimestamp();
    }

    /// <summary>
    /// Sets CSS class for styling.
    /// </summary>
    public void SetCssClass(string? cssClass)
    {
        CssClass = cssClass;
        UpdateTimestamp();
    }

    /// <summary>
    /// Sets tracking ID for analytics.
    /// </summary>
    public void SetTrackingId(string? trackingId)
    {
        TrackingId = trackingId;
        UpdateTimestamp();
    }
}
