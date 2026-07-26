namespace KromicStore.Domain.Entities;

using System.Text.Json;

/// <summary>
/// Represents a theme for storefronts.
/// Themes are platform-wide resources (not tenant-scoped) that all tenants can use.
/// </summary>
public class ThemeEntity : BaseEntity
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
    /// This contains the full theme configuration including default pages,
    /// sections, components, branding, navigation, and footer settings.
    /// </remarks>
    public string DefinitionJson { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether this theme is active and available for use.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Creates a new theme with the provided details.
    /// </summary>
    /// <param name="slug">The unique slug identifier (e.g., "minimal", "modern", "pro").</param>
    /// <param name="name">The display name of the theme.</param>
    /// <param name="description">The description of the theme.</param>
    /// <param name="version">The semantic version (default: "1.0.0").</param>
    /// <param name="definitionJson">The JSON-serialized theme definition.</param>
    /// <returns>A new ThemeEntity instance.</returns>
    public static ThemeEntity Create(
        string slug,
        string name,
        string description,
        string version,
        string definitionJson)
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

        return new ThemeEntity
        {
            Id = Guid.NewGuid(),
            Slug = slug.ToLowerInvariant(),
            Name = name,
            Description = description,
            Version = version,
            DefinitionJson = definitionJson,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Activates this theme, making it available for use by storefronts.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdateTimestamp();
    }

    /// <summary>
    /// Deactivates this theme, preventing new storefronts from using it.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the theme definition JSON.
    /// </summary>
    /// <param name="newDefinitionJson">The new JSON-serialized theme definition.</param>
    public void UpdateDefinition(string newDefinitionJson)
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
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the theme version.
    /// </summary>
    /// <param name="newVersion">The new semantic version.</param>
    public void UpdateVersion(string newVersion)
    {
        if (string.IsNullOrWhiteSpace(newVersion))
            throw new ArgumentException("Version cannot be empty.", nameof(newVersion));

        Version = newVersion;
        UpdateTimestamp();
    }
}
