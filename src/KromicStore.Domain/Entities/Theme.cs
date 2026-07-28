namespace KromicStore.Domain.Entities;

/// <summary>
/// Represents a tenant's theme configuration for storefront customization.
/// </summary>
public class TenantTheme : BaseEntity
{
    /// <summary>Gets the tenant ID this theme belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the theme name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the primary color (hex code).</summary>
    public string PrimaryColor { get; private set; } = string.Empty;

    /// <summary>Gets the secondary color (hex code).</summary>
    public string SecondaryColor { get; private set; } = string.Empty;

    /// <summary>Gets the accent color (hex code).</summary>
    public string AccentColor { get; private set; } = string.Empty;

    /// <summary>Gets the background color (hex code).</summary>
    public string BackgroundColor { get; private set; } = string.Empty;

    /// <summary>Gets the text color (hex code).</summary>
    public string TextColor { get; private set; } = string.Empty;

    /// <summary>Gets the font family.</summary>
    public string FontFamily { get; private set; } = string.Empty;

    /// <summary>Gets the border radius (in pixels).</summary>
    public int BorderRadius { get; private set; }

    /// <summary>Gets the spacing unit (in pixels).</summary>
    public int SpacingUnit { get; private set; }

    /// <summary>Gets the component overrides as JSON.</summary>
    public string ComponentOverrides { get; private set; } = string.Empty;

    /// <summary>Gets the layout options as JSON.</summary>
    public string LayoutOptions { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether this is the active theme for the tenant.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets a value indicating whether this theme is public (available to all tenants) or private (tenant-specific).</summary>
    public bool IsPublic { get; private set; }

    /// <summary>Gets the ID of the tenant who created this theme (null for admin-created public themes).</summary>
    public Guid? CreatedByTenantId { get; private set; }

    /// <summary>Navigation property to the tenant.</summary>
    public Tenant? Tenant { get; private set; }

    /// <summary>
    /// Creates a new instance of TenantTheme with default values.
    /// </summary>
    public static TenantTheme Create(Guid tenantId, string name, bool isPublic = false, Guid? createdByTenantId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Theme name is required.", nameof(name));

        return new TenantTheme
        {
            TenantId = tenantId,
            Name = name,
            PrimaryColor = "#000000",
            SecondaryColor = "#666666",
            AccentColor = "#007bff",
            BackgroundColor = "#ffffff",
            TextColor = "#333333",
            FontFamily = "Inter, sans-serif",
            BorderRadius = 8,
            SpacingUnit = 16,
            ComponentOverrides = "{}",
            LayoutOptions = "{}",
            IsActive = true,
            IsPublic = isPublic,
            CreatedByTenantId = createdByTenantId
        };
    }

    /// <summary>
    /// Updates the color scheme.
    /// </summary>
    public void UpdateColors(string primary, string secondary, string accent, string background, string text)
    {
        if (!string.IsNullOrWhiteSpace(primary))
            PrimaryColor = primary;
        if (!string.IsNullOrWhiteSpace(secondary))
            SecondaryColor = secondary;
        if (!string.IsNullOrWhiteSpace(accent))
            AccentColor = accent;
        if (!string.IsNullOrWhiteSpace(background))
            BackgroundColor = background;
        if (!string.IsNullOrWhiteSpace(text))
            TextColor = text;
    }

    /// <summary>
    /// Updates the typography settings.
    /// </summary>
    public void UpdateTypography(string fontFamily)
    {
        if (!string.IsNullOrWhiteSpace(fontFamily))
            FontFamily = fontFamily;
    }

    /// <summary>
    /// Updates the spacing and border settings.
    /// </summary>
    public void UpdateSpacing(int borderRadius, int spacingUnit)
    {
        if (borderRadius >= 0)
            BorderRadius = borderRadius;
        if (spacingUnit >= 0)
            SpacingUnit = spacingUnit;
    }

    /// <summary>
    /// Updates the component overrides.
    /// </summary>
    public void UpdateComponentOverrides(string overrides)
    {
        if (!string.IsNullOrWhiteSpace(overrides))
            ComponentOverrides = overrides;
    }

    /// <summary>
    /// Updates the layout options.
    /// </summary>
    public void UpdateLayoutOptions(string options)
    {
        if (!string.IsNullOrWhiteSpace(options))
            LayoutOptions = options;
    }

    /// <summary>
    /// Activates this theme for the tenant.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivates this theme for the tenant.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Makes this theme public (available to all tenants).
    /// </summary>
    public void MakePublic()
    {
        IsPublic = true;
        UpdateTimestamp();
    }

    /// <summary>
    /// Makes this theme private (tenant-specific only).
    /// </summary>
    public void MakePrivate()
    {
        IsPublic = false;
        UpdateTimestamp();
    }
}
