#nullable disable

using Xunit;
using KromicStore.Domain.Entities;
using System.Text.Json;

namespace KromicStore.Tests.Unit.Domain;

/// <summary>
/// Unit tests for ThemeEntity validating theme lifecycle and configuration.
/// Tests: creation, JSON validation, version management, and activation.
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

    #region Creation Tests

    [Fact]
    public void Create_WithValidInputs_ReturnsThemeEntity()
    {
        // Arrange
        var slug = "modern-theme";
        var name = "Modern Theme";
        var description = "A modern and responsive theme";
        var version = "1.0.0";

        // Act
        var theme = ThemeEntity.Create(slug, name, description, version, _validThemeJson);

        // Assert
        Assert.NotNull(theme);
        Assert.Equal(slug.ToLowerInvariant(), theme.Slug);
        Assert.Equal(name, theme.Name);
        Assert.Equal(description, theme.Description);
        Assert.Equal(version, theme.Version);
        Assert.True(theme.IsActive);
        Assert.NotEqual(Guid.Empty, theme.Id);
        Assert.Equal(_validThemeJson, theme.DefinitionJson);
    }

    [Fact]
    public void Create_WithNullSlug_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ThemeEntity.Create(null, "Theme", "Desc", "1.0.0", _validThemeJson));
        Assert.Contains("Slug", ex.Message);
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ThemeEntity.Create("theme", "", "Desc", "1.0.0", _validThemeJson));
        Assert.Contains("Name", ex.Message);
    }

    [Fact]
    public void Create_WithNullDefinitionJson_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", null));
        Assert.Contains("Definition JSON", ex.Message);
    }

    [Fact]
    public void Create_WithInvalidJson_ThrowsArgumentException()
    {
        // Arrange
        var invalidJson = "{ not valid json }";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", invalidJson));
        Assert.Contains("not valid JSON", ex.Message);
    }

    [Fact]
    public void Create_WithUppercaseSlug_ConvertsToLowercase()
    {
        // Act
        var theme = ThemeEntity.Create("MODERN-THEME", "Modern Theme", "Desc", "1.0.0", _validThemeJson);

        // Assert
        Assert.Equal("modern-theme", theme.Slug);
    }

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
        var theme = ThemeEntity.Create("complex", "Complex Theme", "Desc", "1.0.0", complexJson);

        // Assert
        Assert.NotNull(theme);
        Assert.Equal(complexJson, theme.DefinitionJson);
    }

    #endregion

    #region Activation Tests

    [Fact]
    public void Activate_DeactivatedTheme_MakesActive()
    {
        // Arrange
        var theme = ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", _validThemeJson);
        theme.Deactivate();

        // Act
        theme.Activate();

        // Assert
        Assert.True(theme.IsActive);
    }

    [Fact]
    public void Deactivate_ActiveTheme_MakesInactive()
    {
        // Arrange
        var theme = ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", _validThemeJson);

        // Act
        theme.Deactivate();

        // Assert
        Assert.False(theme.IsActive);
    }

    [Fact]
    public void Activate_AlreadyActive_RemainsActive()
    {
        // Arrange
        var theme = ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", _validThemeJson);

        // Act
        theme.Activate();

        // Assert
        Assert.True(theme.IsActive);
    }

    [Fact]
    public void Deactivate_AlreadyInactive_RemainsInactive()
    {
        // Arrange
        var theme = ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", _validThemeJson);
        theme.Deactivate();

        // Act
        theme.Deactivate();

        // Assert
        Assert.False(theme.IsActive);
    }

    #endregion

    #region Definition Update Tests

    [Fact]
    public void UpdateDefinition_WithValidJson_UpdatesDefinition()
    {
        // Arrange
        var theme = ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", _validThemeJson);
        var newJson = JsonSerializer.Serialize(new
        {
            pages = new object[] { },
            settings = new { brandColor = "#00FF00" }
        });

        // Act
        theme.UpdateDefinition(newJson);

        // Assert
        Assert.Equal(newJson, theme.DefinitionJson);
    }

    [Fact]
    public void UpdateDefinition_WithNullJson_ThrowsArgumentException()
    {
        // Arrange
        var theme = ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", _validThemeJson);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => theme.UpdateDefinition(null));
        Assert.Contains("Definition JSON", ex.Message);
    }

    [Fact]
    public void UpdateDefinition_WithInvalidJson_ThrowsArgumentException()
    {
        // Arrange
        var theme = ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", _validThemeJson);
        var invalidJson = "{ invalid json }";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => theme.UpdateDefinition(invalidJson));
        Assert.Contains("not valid JSON", ex.Message);
    }

    [Fact]
    public void UpdateDefinition_WithEmptyJson_ThrowsArgumentException()
    {
        // Arrange
        var theme = ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", _validThemeJson);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => theme.UpdateDefinition(""));
        Assert.Contains("Definition JSON", ex.Message);
    }

    [Fact]
    public void UpdateDefinition_WithEmptyJsonObject_Succeeds()
    {
        // Arrange
        var theme = ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", _validThemeJson);
        var emptyJson = "{}";

        // Act
        theme.UpdateDefinition(emptyJson);

        // Assert
        Assert.Equal(emptyJson, theme.DefinitionJson);
    }

    #endregion

    #region Version Update Tests

    [Fact]
    public void UpdateVersion_WithValidVersion_UpdatesVersion()
    {
        // Arrange
        var theme = ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", _validThemeJson);

        // Act
        theme.UpdateVersion("2.0.0");

        // Assert
        Assert.Equal("2.0.0", theme.Version);
    }

    [Fact]
    public void UpdateVersion_WithNullVersion_ThrowsArgumentException()
    {
        // Arrange
        var theme = ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", _validThemeJson);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => theme.UpdateVersion(null));
        Assert.Contains("Version", ex.Message);
    }

    [Fact]
    public void UpdateVersion_WithEmptyVersion_ThrowsArgumentException()
    {
        // Arrange
        var theme = ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", _validThemeJson);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => theme.UpdateVersion(""));
        Assert.Contains("Version", ex.Message);
    }

    [Theory]
    [InlineData("0.0.1")]
    [InlineData("1.0.0")]
    [InlineData("1.0.1")]
    [InlineData("2.5.10")]
    [InlineData("10.20.30")]
    public void UpdateVersion_WithVariousVersionFormats_Succeeds(string version)
    {
        // Arrange
        var theme = ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", _validThemeJson);

        // Act
        theme.UpdateVersion(version);

        // Assert
        Assert.Equal(version, theme.Version);
    }

    #endregion

    #region Timestamp Tests

    [Fact]
    public void UpdateDefinition_UpdatesModifiedTimestamp()
    {
        // Arrange
        var theme = ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", _validThemeJson);
        var originalTimestamp = theme.UpdatedAt;
        System.Threading.Thread.Sleep(10); // Ensure time passes

        // Act
        var newJson = JsonSerializer.Serialize(new { pages = new object[] { } });
        theme.UpdateDefinition(newJson);

        // Assert
        Assert.True(theme.UpdatedAt > originalTimestamp);
    }

    [Fact]
    public void UpdateVersion_UpdatesModifiedTimestamp()
    {
        // Arrange
        var theme = ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", _validThemeJson);
        var originalTimestamp = theme.UpdatedAt;
        System.Threading.Thread.Sleep(10); // Ensure time passes

        // Act
        theme.UpdateVersion("2.0.0");

        // Assert
        Assert.True(theme.UpdatedAt > originalTimestamp);
    }

    [Fact]
    public void Activate_UpdatesModifiedTimestamp()
    {
        // Arrange
        var theme = ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", _validThemeJson);
        theme.Deactivate();
        var originalTimestamp = theme.UpdatedAt;
        System.Threading.Thread.Sleep(10); // Ensure time passes

        // Act
        theme.Activate();

        // Assert
        Assert.True(theme.UpdatedAt > originalTimestamp);
    }

    #endregion

    #region JSON Validation Tests

    [Fact]
    public void Create_WithArrayJson_Succeeds()
    {
        // Arrange
        var arrayJson = "[]";

        // Act
        var theme = ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", arrayJson);

        // Assert
        Assert.Equal(arrayJson, theme.DefinitionJson);
    }

    [Fact]
    public void Create_WithNumberJson_Succeeds()
    {
        // Arrange
        var numberJson = "42";

        // Act
        var theme = ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", numberJson);

        // Assert
        Assert.Equal(numberJson, theme.DefinitionJson);
    }

    [Fact]
    public void Create_WithStringJson_Succeeds()
    {
        // Arrange
        var stringJson = "\"hello\"";

        // Act
        var theme = ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", stringJson);

        // Assert
        Assert.Equal(stringJson, theme.DefinitionJson);
    }

    [Fact]
    public void Create_WithNestedJson_Succeeds()
    {
        // Arrange
        var nestedJson = JsonSerializer.Serialize(new
        {
            level1 = new
            {
                level2 = new
                {
                    level3 = new
                    {
                        value = "deep nested"
                    }
                }
            }
        });

        // Act
        var theme = ThemeEntity.Create("theme", "Theme", "Desc", "1.0.0", nestedJson);

        // Assert
        Assert.Equal(nestedJson, theme.DefinitionJson);
    }

    #endregion
}
