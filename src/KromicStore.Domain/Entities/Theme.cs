namespace KromicStore.Domain.Entities;

using System.Text.Json;

/// <summary>
/// Represents a unified theme entity for storefront customization.
/// 
/// Architecture:
/// - Platform Themes: Created by SuperUser (TenantId = null, IsPublic = true)
/// - Tenant Themes: Created by Tenant (TenantId = tenantId, IsPublic = false/true)
/// - Cloned Themes: Tenant-created copy of platform/shared themes (SourceThemeId != null)
/// 
/// A single entity consolidates platform templates and tenant customizations.
/// </summary>
public class Theme : BaseEntity
{
    /// <summary>Gets the unique slug identifier for the theme.</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>Gets the display name of the theme.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the description of the theme.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Gets the semantic version of the theme.</summary>
    public string Version { get; private set; } = "1.0.0";

    /// <summary>Gets the complete theme definition stored as JSON.</summary>
    /// <remarks>
    /// This contains the full theme configuration including colors, fonts, layouts,
    /// default pages, sections, components, branding, navigation, and footer settings.
    /// Can be used as a fallback or as the primary definition.
    /// </remarks>
    public string DefinitionJson { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether this theme is public (available to other tenants).</summary>
    public bool IsPublic { get; private set; }

    /// <summary>Gets the ID of the theme this was cloned from (null if original or platform theme).</summary>
    public Guid? SourceThemeId { get; private set; }

    /// <summary>Gets the ID of the tenant who owns/created this theme (null for SuperUser-created platform themes).</summary>
    public Guid? OwnerTenantId { get; private set; }

    /// <summary>Gets the ID of the user who created this theme.</summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>Gets the ID of the user who last modified this theme.</summary>
    public Guid? LastModifiedByUserId { get; private set; }

    /// <summary>Gets a value indicating whether this theme is active and available for use.</summary>
    public bool IsActive { get; private set; } = true;

    // Legacy individual color/font fields (for backward compatibility, gradually phase out)
    // These are optional and can be populated from DefinitionJson if needed

    /// <summary>Gets the primary color (hex code) - legacy field.</summary>
    [Obsolete("Use DefinitionJson instead. This field is for backward compatibility only.")]
    public string? PrimaryColor { get; private set; }

    /// <summary>Gets the secondary color (hex code) - legacy field.</summary>
    [Obsolete("Use DefinitionJson instead. This field is for backward compatibility only.")]
    public string? SecondaryColor { get; private set; }

    /// <summary>Navigation property to the owning tenant.</summary>
    public Tenant? Tenant { get; private set; }

    /// <summary>
    /// Creates a new platform theme (SuperUser only).
    /// </summary>
    public static Theme CreatePlatformTheme(
        string slug,
        string name,
        string description,
        string version,
        string definitionJson,
        Guid createdByUserId)
    {
        ValidateThemeInput(slug, name, definitionJson);

        return new Theme
        {
            Id = Guid.NewGuid(),
            Slug = slug.ToLowerInvariant(),
            Name = name,
            Description = description,
            Version = version,
            DefinitionJson = definitionJson,
            IsPublic = true,
            SourceThemeId = null,
            OwnerTenantId = null,
            CreatedByUserId = createdByUserId,
            LastModifiedByUserId = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a new tenant-owned theme.
    /// </summary>
    public static Theme CreateTenantTheme(
        Guid tenantId,
        string name,
        string definitionJson,
        bool isPublic,
        Guid createdByUserId,
        Guid? sourceThemeId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        ValidateThemeInput(Guid.NewGuid().ToString("N").Substring(0, 8), name, definitionJson);

        // Generate unique slug: {tenantId}-{name}-{random suffix}
        var namePart = name.ToLowerInvariant().Replace(" ", "-");
        var randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 6);
        var slug = $"{tenantId:N}-{namePart}-{randomSuffix}";

        return new Theme
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = name,
            Description = string.Empty,
            Version = "1.0.0",
            DefinitionJson = definitionJson,
            IsPublic = isPublic,
            SourceThemeId = sourceThemeId,
            OwnerTenantId = tenantId,
            CreatedByUserId = createdByUserId,
            LastModifiedByUserId = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Clones an existing theme (tenant-specific clone).
    /// </summary>
    public Theme Clone(Guid tenantId, Guid createdByUserId, string? newName = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        // Generate unique slug: {tenantId}-{name}-{random suffix}
        var namePart = (newName ?? Name).ToLowerInvariant().Replace(" ", "-");
        var randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 6);
        var slug = $"{tenantId:N}-{namePart}-{randomSuffix}";

        return new Theme
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = newName ?? $"{Name} (Clone)",
            Description = Description,
            Version = Version,
            DefinitionJson = DefinitionJson,
            IsPublic = false, // Cloned themes default to private
            SourceThemeId = this.Id,
            OwnerTenantId = tenantId,
            CreatedByUserId = createdByUserId,
            LastModifiedByUserId = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates the theme definition JSON.
    /// </summary>
    public void UpdateDefinition(string newDefinitionJson, Guid modifiedByUserId)
    {
        if (string.IsNullOrWhiteSpace(newDefinitionJson))
            throw new ArgumentException("Definition JSON cannot be empty.", nameof(newDefinitionJson));

        // Validate JSON
        try
        {
            JsonDocument.Parse(newDefinitionJson);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Definition JSON is not valid JSON.", nameof(newDefinitionJson), ex);
        }

        DefinitionJson = newDefinitionJson;
        LastModifiedByUserId = modifiedByUserId;
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the theme metadata (name, description, version).
    /// </summary>
    public void UpdateMetadata(string name, string? description, string? version, Guid modifiedByUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        Name = name;
        if (!string.IsNullOrWhiteSpace(description))
            Description = description;
        if (!string.IsNullOrWhiteSpace(version))
            Version = version;

        LastModifiedByUserId = modifiedByUserId;
        UpdateTimestamp();
    }

    /// <summary>
    /// Makes this theme public (available to other tenants).
    /// </summary>
    public void MakePublic(Guid modifiedByUserId)
    {
        IsPublic = true;
        LastModifiedByUserId = modifiedByUserId;
        UpdateTimestamp();
    }

    /// <summary>
    /// Makes this theme private (tenant-specific only).
    /// </summary>
    public void MakePrivate(Guid modifiedByUserId)
    {
        IsPublic = false;
        LastModifiedByUserId = modifiedByUserId;
        UpdateTimestamp();
    }

    /// <summary>
    /// Activates this theme.
    /// </summary>
    public void Activate(Guid modifiedByUserId)
    {
        IsActive = true;
        LastModifiedByUserId = modifiedByUserId;
        UpdateTimestamp();
    }

    /// <summary>
    /// Deactivates this theme.
    /// </summary>
    public void Deactivate(Guid modifiedByUserId)
    {
        IsActive = false;
        LastModifiedByUserId = modifiedByUserId;
        UpdateTimestamp();
    }

    /// <summary>
    /// Validates theme input parameters.
    /// </summary>
    private static void ValidateThemeInput(string slug, string name, string definitionJson)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug cannot be empty.", nameof(slug));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(definitionJson))
            throw new ArgumentException("Definition JSON cannot be empty.", nameof(definitionJson));

        // Validate JSON
        try
        {
            JsonDocument.Parse(definitionJson);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Definition JSON is not valid JSON.", nameof(definitionJson), ex);
        }
    }
}
