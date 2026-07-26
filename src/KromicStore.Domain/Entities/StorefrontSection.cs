namespace KromicStore.Domain.Entities;

/// <summary>
/// Represents a section within a storefront page.
/// A section is a container for multiple components organized together.
/// </summary>
public class StorefrontSection : BaseEntity
{
    /// <summary>Gets the tenant ID this section belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the parent page ID.</summary>
    public Guid PageId { get; private set; }

    /// <summary>Gets the parent page.</summary>
    public StorefrontPage? Page { get; private set; }

    /// <summary>Gets the section name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the section description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets a value indicating whether the section is visible.</summary>
    public bool IsVisible { get; private set; } = true;

    /// <summary>Gets the display order within the page.</summary>
    public int DisplayOrder { get; private set; }

    /// <summary>Gets an optional CSS class for styling.</summary>
    public string? CssClass { get; private set; }

    /// <summary>Gets the background color (hex).</summary>
    public string? BackgroundColor { get; private set; }

    /// <summary>Gets the background image URL.</summary>
    public string? BackgroundImageUrl { get; private set; }

    /// <summary>Gets the components in this section.</summary>
    public ICollection<StorefrontComponent> Components { get; private set; } = new List<StorefrontComponent>();

    /// <summary>
    /// Creates a new instance of StorefrontSection.
    /// </summary>
    public static StorefrontSection Create(
        Guid tenantId,
        Guid pageId,
        string name,
        int displayOrder = 0,
        string? description = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (pageId == Guid.Empty)
            throw new ArgumentException("Page ID is required.", nameof(pageId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Section name is required.", nameof(name));

        return new StorefrontSection
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PageId = pageId,
            Name = name,
            Description = description,
            DisplayOrder = displayOrder,
            IsVisible = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates section information.
    /// </summary>
    public void Update(string name, string? description = null, string? cssClass = null, 
        string? backgroundColor = null, string? backgroundImageUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name;

        Description = description;
        CssClass = cssClass;
        BackgroundColor = backgroundColor;
        BackgroundImageUrl = backgroundImageUrl;

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
    /// Shows the section.
    /// </summary>
    public void Show()
    {
        IsVisible = true;
        UpdateTimestamp();
    }

    /// <summary>
    /// Hides the section.
    /// </summary>
    public void Hide()
    {
        IsVisible = false;
        UpdateTimestamp();
    }

    /// <summary>
    /// Toggles visibility of the section.
    /// </summary>
    public void ToggleVisibility()
    {
        IsVisible = !IsVisible;
        UpdateTimestamp();
    }

    /// <summary>
    /// Adds a component to the section.
    /// </summary>
    public void AddComponent(StorefrontComponent component)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));
        if (component.TenantId != TenantId)
            throw new InvalidOperationException("Component must belong to the same tenant.");
        if (component.SectionId != Id)
            throw new InvalidOperationException("Component section ID must match this section.");

        Components.Add(component);
        UpdateTimestamp();
    }

    /// <summary>
    /// Removes a component from the section.
    /// </summary>
    public void RemoveComponent(Guid componentId)
    {
        var component = Components.FirstOrDefault(c => c.Id == componentId);
        if (component != null)
        {
            Components.Remove(component);
            UpdateTimestamp();
        }
    }

    /// <summary>
    /// Gets a component by ID.
    /// </summary>
    public StorefrontComponent? GetComponent(Guid componentId)
    {
        return Components.FirstOrDefault(c => c.Id == componentId);
    }

    /// <summary>
    /// Gets all visible components ordered by display order.
    /// </summary>
    public IEnumerable<StorefrontComponent> GetVisibleComponents()
    {
        return Components.Where(c => c.IsVisible).OrderBy(c => c.DisplayOrder);
    }

    /// <summary>
    /// Updates component order.
    /// </summary>
    public void ReorderComponents(IDictionary<Guid, int> componentOrders)
    {
        if (componentOrders == null)
            throw new ArgumentNullException(nameof(componentOrders));

        foreach (var component in Components)
        {
            if (componentOrders.TryGetValue(component.Id, out var newOrder))
            {
                component.SetDisplayOrder(newOrder);
            }
        }

        UpdateTimestamp();
    }
}
