#nullable disable

using Xunit;
using KromicStore.Domain.Entities;
using System.Text.Json;

namespace KromicStore.Tests.Unit.Domain;

/// <summary>
/// Unit tests for Theme entity validating platform theme lifecycle and configuration.
/// Tests: creation, JSON validation, version management, activation, and cloning.
/// </summary>
public class ThemeEntityTests
{
    private readonly string _validThemeJson = JsonSerializer.Serialize(new
    {
        pages = new object[] { },
        settings = new
        {
            brandColor = "#FF0000",
            fontFamily = "Arial"
        }
    });

    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testTenantId = Guid.NewGuid();

    #region Platform Theme Creation Tests

    [Fact]
    public void CreatePlatformTheme_WithValidInputs_ReturnsTheme()
    {
        // Arrange
        var slug = "modern-theme";
        var name = "Modern Theme";
        var description = "A modern and responsive theme";
        var version = "1.0.0";

        // Act
        var theme = Theme.CreatePlatformTheme(slug, name, description, version, _validThemeJson, _testUserId);

        // Assert
        Assert.NotNull(theme);
        Assert.Equal(slug.ToLowerInvariant(), theme.Slug);
        Assert.Equal(name, theme.Name);
        Assert.Equal(description, theme.Description);
        Assert.Equal(version, theme.Version);
        Assert.True(theme.IsActive);
        Assert.True(theme.IsPublic); // Platform themes are public
        Assert.Null(theme.OwnerTenantId); // Platform themes have no owner tenant
        Assert.NotEqual(Guid.Empty, theme.Id);
        Assert.Equal(_validThemeJson, theme.DefinitionJson);
        Assert.Equal(_testUserId, theme.CreatedByUserId);
    }

    [Fact]
    public void CreatePlatformTheme_WithNullSlug_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            Theme.CreatePlatformTheme(null, "Theme", "Desc", "1.0.0", _validThemeJson, _testUserId));
        Assert.Contains("Slug", ex.Message);
    }

    [Fact]
    public void CreatePlatformTheme_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            Theme.CreatePlatformTheme("theme", "", "Desc", "1.0.0", _validThemeJson, _testUserId));
        Assert.Contains("Name", ex.Message);
    }

    [Fact]
    public void CreatePlatformTheme_WithNullDefinitionJson_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            Theme.CreatePlatformTheme("theme", "Theme", "Desc", "1.0.0", null, _testUserId));
        Assert.Contains("Definition JSON", ex.Message);
    }

    [Fact]
    public void CreatePlatformTheme_WithInvalidJson_ThrowsArgumentException()
    {
        // Arrange
        var invalidJson = "{ not valid json }";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            Theme.CreatePlatformTheme("theme", "Theme", "Desc", "1.0.0", invalidJson, _testUserId));
        Assert.Contains("not valid JSON", ex.Message);
    }

    #endregion

    #region Tenant Theme Creation Tests

    [Fact]
    public void CreateTenantTheme_WithValidInputs_ReturnsTheme()
    {
        // Arrange
        var name = "Custom Theme";
        var version = "1.0.0";

        // Act
        var theme = Theme.CreateTenantTheme(_testTenantId, name, _validThemeJson, isPublic: false, _testUserId);

        // Assert
        Assert.NotNull(theme);
        Assert.Equal(_testTenantId, theme.OwnerTenantId);
        Assert.Equal(_testTenantId, theme.OwnerTenantId);
        Assert.Equal(name, theme.Name);
        Assert.False(theme.IsPublic); // Private by default
        Assert.True(theme.IsActive);
        Assert.Null(theme.SourceThemeId); // Not cloned
        Assert.Equal(_testUserId, theme.CreatedByUserId);
    }

    [Fact]
    public void CreateTenantTheme_WithEmptyTenantId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            Theme.CreateTenantTheme(Guid.Empty, "Theme", _validThemeJson, false, _testUserId));
        Assert.Contains("Tenant ID", ex.Message);
    }

    [Fact]
    public void CreateTenantTheme_AsPublic_MakesPublic()
    {
        // Act
        var theme = Theme.CreateTenantTheme(_testTenantId, "Shared Theme", _validThemeJson, isPublic: true, _testUserId);

        // Assert
        Assert.True(theme.IsPublic);
    }

    #endregion

    #region Theme Cloning Tests

    [Fact]
    public void Clone_CreatesNewThemeWithSourceReference()
    {
        // Arrange
        var platformTheme = Theme.CreatePlatformTheme("original", "Original", "Desc", "1.0.0", _validThemeJson, _testUserId);

        // Act
        var clonedTheme = platformTheme.Clone(_testTenantId, _testUserId, "Cloned Theme");

        // Assert
        Assert.NotNull(clonedTheme);
        Assert.Equal(_testTenantId, clonedTheme.OwnerTenantId);
        Assert.Equal(_testTenantId, clonedTheme.OwnerTenantId);
        Assert.Equal("Cloned Theme", clonedTheme.Name);
        Assert.Equal(platformTheme.Id, clonedTheme.SourceThemeId);
        Assert.Equal(platformTheme.DefinitionJson, clonedTheme.DefinitionJson);
        Assert.False(clonedTheme.IsPublic); // Cloned themes default to private
    }

    [Fact]
    public void Clone_WithoutNewName_UsesDefaultName()
    {
        // Arrange
        var platformTheme = Theme.CreatePlatformTheme("original", "Original", "Desc", "1.0.0", _validThemeJson, _testUserId);

        // Act
        var clonedTheme = platformTheme.Clone(_testTenantId, _testUserId);

        // Assert
        Assert.Contains("Clone", clonedTheme.Name);
    }

    [Fact]
    public void Clone_WithEmptyTenantId_ThrowsArgumentException()
    {
        // Arrange
        var theme = Theme.CreatePlatformTheme("theme", "Theme", "Desc", "1.0.0", _validThemeJson, _testUserId);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => theme.Clone(Guid.Empty, _testUserId));
        Assert.Contains("Tenant ID", ex.Message);
    }

    #endregion

    #region Theme Updates Tests

    [Fact]
    public void UpdateDefinition_WithValidJson_UpdatesDefinition()
    {
        // Arrange
        var theme = Theme.CreatePlatformTheme("theme", "Theme", "Desc", "1.0.0", _validThemeJson, _testUserId);
        var newJson = JsonSerializer.Serialize(new
        {
            pages = new object[] { },
            settings = new { brandColor = "#00FF00" }
        });

        // Act
        theme.UpdateDefinition(newJson, _testUserId);

        // Assert
        Assert.Equal(newJson, theme.DefinitionJson);
        Assert.Equal(_testUserId, theme.LastModifiedByUserId);
    }

    [Fact]
    public void UpdateMetadata_UpdatesNameAndDescription()
    {
        // Arrange
        var theme = Theme.CreatePlatformTheme("theme", "Theme", "Desc", "1.0.0", _validThemeJson, _testUserId);

        // Act
        theme.UpdateMetadata("New Name", "New Desc", "2.0.0", _testUserId);

        // Assert
        Assert.Equal("New Name", theme.Name);
        Assert.Equal("New Desc", theme.Description);
        Assert.Equal("2.0.0", theme.Version);
        Assert.Equal(_testUserId, theme.LastModifiedByUserId);
    }

    #endregion

    #region Activation Tests

    [Fact]
    public void Activate_DeactivatedTheme_MakesActive()
    {
        // Arrange
        var theme = Theme.CreatePlatformTheme("theme", "Theme", "Desc", "1.0.0", _validThemeJson, _testUserId);
        theme.Deactivate(_testUserId);

        // Act
        theme.Activate(_testUserId);

        // Assert
        Assert.True(theme.IsActive);
    }

    [Fact]
    public void Deactivate_ActiveTheme_MakesInactive()
    {
        // Arrange
        var theme = Theme.CreatePlatformTheme("theme", "Theme", "Desc", "1.0.0", _validThemeJson, _testUserId);

        // Act
        theme.Deactivate(_testUserId);

        // Assert
        Assert.False(theme.IsActive);
    }

    #endregion

    #region Visibility Tests

    [Fact]
    public void MakePublic_MakesThemePublic()
    {
        // Arrange
        var theme = Theme.CreateTenantTheme(_testTenantId, "Theme", _validThemeJson, isPublic: false, _testUserId);

        // Act
        theme.MakePublic(_testUserId);

        // Assert
        Assert.True(theme.IsPublic);
    }

    [Fact]
    public void MakePrivate_MakesThemePrivate()
    {
        // Arrange
        var theme = Theme.CreateTenantTheme(_testTenantId, "Theme", _validThemeJson, isPublic: true, _testUserId);

        // Act
        theme.MakePrivate(_testUserId);

        // Assert
        Assert.False(theme.IsPublic);
    }

    #endregion

    #region JSON Validation Tests

    [Fact]
    public void Create_WithComplexValidJson_Succeeds()
    {
        // Arrange
        var complexJson = JsonSerializer.Serialize(new
        {
            pages = new object[]
            {
                new { name = "home", slug = "home" },
                new { name = "about", slug = "about" }
            },
            settings = new
            {
                brandColor = "#FF0000",
                fontFamily = "Arial",
                colors = new { primary = "#FF0000", secondary = "#00FF00" }
            },
            footer = new { copyright = "2024" }
        });

        // Act
        var theme = Theme.CreatePlatformTheme("complex", "Complex Theme", "Desc", "1.0.0", complexJson, _testUserId);

        // Assert
        Assert.NotNull(theme);
        Assert.Equal(complexJson, theme.DefinitionJson);
    }

    [Fact]
    public void Create_WithArrayJson_Succeeds()
    {
        // Arrange
        var arrayJson = "[]";

        // Act
        var theme = Theme.CreatePlatformTheme("theme", "Theme", "Desc", "1.0.0", arrayJson, _testUserId);

        // Assert
        Assert.Equal(arrayJson, theme.DefinitionJson);
    }

    [Fact]
    public void Create_WithEmptyObjectJson_Succeeds()
    {
        // Arrange
        var emptyJson = "{}";

        // Act
        var theme = Theme.CreatePlatformTheme("theme", "Theme", "Desc", "1.0.0", emptyJson, _testUserId);

        // Assert
        Assert.Equal(emptyJson, theme.DefinitionJson);
    }

    #endregion

    #region Timestamp Tests

    [Fact]
    public void UpdateDefinition_UpdatesModifiedTimestamp()
    {
        // Arrange
        var theme = Theme.CreatePlatformTheme("theme", "Theme", "Desc", "1.0.0", _validThemeJson, _testUserId);
        var originalTimestamp = theme.UpdatedAt;
        System.Threading.Thread.Sleep(10); // Ensure time passes

        // Act
        var newJson = JsonSerializer.Serialize(new { pages = new object[] { } });
        theme.UpdateDefinition(newJson, _testUserId);

        // Assert
        Assert.True(theme.UpdatedAt > originalTimestamp);
    }

    #endregion
}
