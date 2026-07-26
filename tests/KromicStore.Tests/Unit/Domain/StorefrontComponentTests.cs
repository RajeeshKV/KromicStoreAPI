#nullable disable

using Xunit;
using KromicStore.Domain.Entities;
using KromicStore.Domain.ValueObjects;
using KromicStore.Domain.Enums;

namespace KromicStore.Tests.Unit.Domain;

/// <summary>
/// Unit tests for StorefrontComponent entity validating component lifecycle and configuration.
/// Tests: creation, visibility toggling, config updates, and styling.
/// </summary>
public class StorefrontComponentTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _sectionId = Guid.NewGuid();

    #region Creation Tests

    [Fact]
    public void Create_WithValidInputs_ReturnsComponentEntity()
    {
        // Arrange
        var type = ComponentType.Hero;
        var config = ComponentConfig.CreateHero("Welcome", "To our store");

        // Act
        var component = StorefrontComponent.Create(_tenantId, _sectionId, type, config);

        // Assert
        Assert.NotNull(component);
        Assert.Equal(_tenantId, component.TenantId);
        Assert.Equal(_sectionId, component.SectionId);
        Assert.Equal(type, component.Type);
        Assert.Equal(config, component.Config);
        Assert.True(component.IsVisible);
        Assert.Equal(0, component.DisplayOrder);
        Assert.NotEqual(Guid.Empty, component.Id);
    }

    [Fact]
    public void Create_WithDisplayOrder_IncludesDisplayOrder()
    {
        // Arrange
        var type = ComponentType.Banner;
        var config = ComponentConfig.CreateBanner("Promo");
        var displayOrder = 5;

        // Act
        var component = StorefrontComponent.Create(_tenantId, _sectionId, type, config, displayOrder);

        // Assert
        Assert.Equal(displayOrder, component.DisplayOrder);
    }

    [Fact]
    public void Create_WithEmptyTenantId_ThrowsArgumentException()
    {
        // Arrange
        var config = ComponentConfig.CreateHero("Test");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            StorefrontComponent.Create(Guid.Empty, _sectionId, ComponentType.Hero, config));
        Assert.Contains("Tenant ID", ex.Message);
    }

    [Fact]
    public void Create_WithEmptySectionId_ThrowsArgumentException()
    {
        // Arrange
        var config = ComponentConfig.CreateHero("Test");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            StorefrontComponent.Create(_tenantId, Guid.Empty, ComponentType.Hero, config));
        Assert.Contains("Section ID", ex.Message);
    }

    #endregion

    #region Visibility Tests

    [Fact]
    public void Show_HiddenComponent_MakesVisible()
    {
        // Arrange
        var config = ComponentConfig.CreateHero("Welcome");
        var component = StorefrontComponent.Create(_tenantId, _sectionId, ComponentType.Hero, config);
        component.Hide();

        // Act
        component.Show();

        // Assert
        Assert.True(component.IsVisible);
    }

    [Fact]
    public void Hide_VisibleComponent_MakesNotVisible()
    {
        // Arrange
        var config = ComponentConfig.CreateHero("Welcome");
        var component = StorefrontComponent.Create(_tenantId, _sectionId, ComponentType.Hero, config);

        // Act
        component.Hide();

        // Assert
        Assert.False(component.IsVisible);
    }

    [Fact]
    public void ToggleVisibility_TogglesState()
    {
        // Arrange
        var config = ComponentConfig.CreateHero("Welcome");
        var component = StorefrontComponent.Create(_tenantId, _sectionId, ComponentType.Hero, config);
        var initialVisibility = component.IsVisible;

        // Act
        component.ToggleVisibility();
        var afterToggle = component.IsVisible;

        // Assert
        Assert.NotEqual(initialVisibility, afterToggle);
    }

    [Fact]
    public void ToggleVisibility_ToggledMultipleTimes_AlternatesState()
    {
        // Arrange
        var config = ComponentConfig.CreateHero("Welcome");
        var component = StorefrontComponent.Create(_tenantId, _sectionId, ComponentType.Hero, config);

        // Act & Assert
        Assert.True(component.IsVisible);
        
        component.ToggleVisibility();
        Assert.False(component.IsVisible);
        
        component.ToggleVisibility();
        Assert.True(component.IsVisible);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void UpdateConfig_WithValidConfig_UpdatesConfiguration()
    {
        // Arrange
        var initialConfig = ComponentConfig.CreateHero("Welcome", "To our store");
        var component = StorefrontComponent.Create(_tenantId, _sectionId, ComponentType.Hero, initialConfig);
        var newConfig = ComponentConfig.CreateHero("Updated Title", "New subtitle");

        // Act
        component.UpdateConfig(newConfig);

        // Assert
        Assert.Equal(newConfig, component.Config);
    }

    [Fact]
    public void UpdateConfig_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        var config = ComponentConfig.CreateHero("Welcome");
        var component = StorefrontComponent.Create(_tenantId, _sectionId, ComponentType.Hero, config);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => component.UpdateConfig(null));
    }

    [Fact]
    public void UpdateConfig_WithDifferentType_UpdatesSuccessfully()
    {
        // Arrange
        var initialConfig = ComponentConfig.CreateHero("Welcome");
        var component = StorefrontComponent.Create(_tenantId, _sectionId, ComponentType.Hero, initialConfig);
        var bannerConfig = ComponentConfig.CreateBanner("Promo Banner");

        // Act
        component.UpdateConfig(bannerConfig);

        // Assert
        Assert.Equal(bannerConfig, component.Config);
        Assert.Equal(ComponentType.Banner, component.Config.Type);
    }

    #endregion

    #region Display Order Tests

    [Fact]
    public void SetDisplayOrder_WithValidOrder_UpdatesOrder()
    {
        // Arrange
        var config = ComponentConfig.CreateHero("Welcome");
        var component = StorefrontComponent.Create(_tenantId, _sectionId, ComponentType.Hero, config);

        // Act
        component.SetDisplayOrder(5);

        // Assert
        Assert.Equal(5, component.DisplayOrder);
    }

    [Fact]
    public void SetDisplayOrder_WithNegativeOrder_ThrowsArgumentException()
    {
        // Arrange
        var config = ComponentConfig.CreateHero("Welcome");
        var component = StorefrontComponent.Create(_tenantId, _sectionId, ComponentType.Hero, config);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => component.SetDisplayOrder(-1));
        Assert.Contains("negative", ex.Message.ToLower());
    }

    [Fact]
    public void SetDisplayOrder_WithZero_Succeeds()
    {
        // Arrange
        var config = ComponentConfig.CreateHero("Welcome");
        var component = StorefrontComponent.Create(_tenantId, _sectionId, ComponentType.Hero, config);
        component.SetDisplayOrder(5);

        // Act
        component.SetDisplayOrder(0);

        // Assert
        Assert.Equal(0, component.DisplayOrder);
    }

    #endregion

    #region Styling Tests

    [Fact]
    public void SetCssClass_WithValidClass_SetsCssClass()
    {
        // Arrange
        var config = ComponentConfig.CreateHero("Welcome");
        var component = StorefrontComponent.Create(_tenantId, _sectionId, ComponentType.Hero, config);

        // Act
        component.SetCssClass("hero-large");

        // Assert
        Assert.Equal("hero-large", component.CssClass);
    }

    [Fact]
    public void SetCssClass_WithNull_ClearsCssClass()
    {
        // Arrange
        var config = ComponentConfig.CreateHero("Welcome");
        var component = StorefrontComponent.Create(_tenantId, _sectionId, ComponentType.Hero, config);
        component.SetCssClass("hero-large");

        // Act
        component.SetCssClass(null);

        // Assert
        Assert.Null(component.CssClass);
    }

    [Fact]
    public void SetCssClass_WithEmptyString_SetsCssClass()
    {
        // Arrange
        var config = ComponentConfig.CreateHero("Welcome");
        var component = StorefrontComponent.Create(_tenantId, _sectionId, ComponentType.Hero, config);

        // Act
        component.SetCssClass("");

        // Assert
        Assert.Empty(component.CssClass);
    }

    #endregion

    #region Tracking Tests

    [Fact]
    public void SetTrackingId_WithValidId_SetsTrackingId()
    {
        // Arrange
        var config = ComponentConfig.CreateHero("Welcome");
        var component = StorefrontComponent.Create(_tenantId, _sectionId, ComponentType.Hero, config);

        // Act
        component.SetTrackingId("hero-cta-button");

        // Assert
        Assert.Equal("hero-cta-button", component.TrackingId);
    }

    [Fact]
    public void SetTrackingId_WithNull_ClearsTrackingId()
    {
        // Arrange
        var config = ComponentConfig.CreateHero("Welcome");
        var component = StorefrontComponent.Create(_tenantId, _sectionId, ComponentType.Hero, config);
        component.SetTrackingId("hero-cta");

        // Act
        component.SetTrackingId(null);

        // Assert
        Assert.Null(component.TrackingId);
    }

    #endregion

    #region Component Type Tests

    [Theory]
    [InlineData(ComponentType.Hero)]
    [InlineData(ComponentType.Banner)]
    [InlineData(ComponentType.ProductGrid)]
    [InlineData(ComponentType.CategoryGrid)]
    [InlineData(ComponentType.Newsletter)]
    [InlineData(ComponentType.TextBlock)]
    [InlineData(ComponentType.ImageBlock)]
    [InlineData(ComponentType.ButtonBlock)]
    public void Create_WithVariousComponentTypes_Succeeds(ComponentType type)
    {
        // Arrange
        ComponentConfig config = type switch
        {
            ComponentType.Hero => ComponentConfig.CreateHero("Title"),
            ComponentType.Banner => ComponentConfig.CreateBanner("Banner"),
            ComponentType.ProductGrid => ComponentConfig.CreateProductGrid(),
            ComponentType.CategoryGrid => ComponentConfig.CreateCategoryGrid(),
            ComponentType.Newsletter => ComponentConfig.CreateNewsletter("Subscribe"),
            ComponentType.TextBlock => ComponentConfig.CreateTextBlock("Some text"),
            ComponentType.ImageBlock => ComponentConfig.CreateImageBlock("https://example.com/image.jpg"),
            ComponentType.ButtonBlock => ComponentConfig.CreateButtonBlock("Click", "https://example.com"),
            _ => ComponentConfig.CreateHero("Default")
        };

        // Act
        var component = StorefrontComponent.Create(_tenantId, _sectionId, type, config);

        // Assert
        Assert.NotNull(component);
        Assert.Equal(type, component.Type);
    }

    #endregion
}
