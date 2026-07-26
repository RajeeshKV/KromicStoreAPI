#nullable disable

using Xunit;
using KromicStore.Domain.Entities;
using KromicStore.Domain.Enums;
using KromicStore.Domain.ValueObjects;

namespace KromicStore.Tests.Unit.Domain;

/// <summary>
/// Unit tests for StorefrontSection entity validating section lifecycle and component management.
/// Tests: creation, visibility toggling, and component management.
/// </summary>
public class StorefrontSectionTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _pageId = Guid.NewGuid();

    #region Creation Tests

    [Fact]
    public void Create_WithValidInputs_ReturnsSectionEntity()
    {
        // Arrange
        var name = "Hero Section";

        // Act
        var section = StorefrontSection.Create(_tenantId, _pageId, name);

        // Assert
        Assert.NotNull(section);
        Assert.Equal(_tenantId, section.TenantId);
        Assert.Equal(_pageId, section.PageId);
        Assert.Equal(name, section.Name);
        Assert.True(section.IsVisible);
        Assert.Equal(0, section.DisplayOrder);
        Assert.NotEqual(Guid.Empty, section.Id);
        Assert.Empty(section.Components);
    }

    [Fact]
    public void Create_WithDisplayOrderAndDescription_IncludesOptionalFields()
    {
        // Arrange
        var name = "Hero Section";
        var displayOrder = 5;
        var description = "Main hero section with CTA";

        // Act
        var section = StorefrontSection.Create(_tenantId, _pageId, name, displayOrder, description);

        // Assert
        Assert.Equal(displayOrder, section.DisplayOrder);
        Assert.Equal(description, section.Description);
    }

    [Fact]
    public void Create_WithEmptyTenantId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            StorefrontSection.Create(Guid.Empty, _pageId, "Hero"));
        Assert.Contains("Tenant ID", ex.Message);
    }

    [Fact]
    public void Create_WithEmptyPageId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            StorefrontSection.Create(_tenantId, Guid.Empty, "Hero"));
        Assert.Contains("Page ID", ex.Message);
    }

    [Fact]
    public void Create_WithNullName_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            StorefrontSection.Create(_tenantId, _pageId, null));
        Assert.Contains("name", ex.Message.ToLower());
    }

    #endregion

    #region Visibility Tests

    [Fact]
    public void Show_HidesSection_MakesVisible()
    {
        // Arrange
        var section = StorefrontSection.Create(_tenantId, _pageId, "Hero");
        section.Hide();

        // Act
        section.Show();

        // Assert
        Assert.True(section.IsVisible);
    }

    [Fact]
    public void Hide_MakesSection_NotVisible()
    {
        // Arrange
        var section = StorefrontSection.Create(_tenantId, _pageId, "Hero");

        // Act
        section.Hide();

        // Assert
        Assert.False(section.IsVisible);
    }

    [Fact]
    public void ToggleVisibility_TogglesState()
    {
        // Arrange
        var section = StorefrontSection.Create(_tenantId, _pageId, "Hero");
        var initialVisibility = section.IsVisible;

        // Act
        section.ToggleVisibility();
        var afterToggle = section.IsVisible;

        // Assert
        Assert.NotEqual(initialVisibility, afterToggle);
    }

    [Fact]
    public void ToggleVisibility_ToggledMultipleTimes_AlternatesState()
    {
        // Arrange
        var section = StorefrontSection.Create(_tenantId, _pageId, "Hero");

        // Act & Assert
        Assert.True(section.IsVisible);
        
        section.ToggleVisibility();
        Assert.False(section.IsVisible);
        
        section.ToggleVisibility();
        Assert.True(section.IsVisible);
    }

    #endregion

    #region Component Management Tests

    [Fact]
    public void AddComponent_WithValidComponent_AddsToCollection()
    {
        // Arrange
        var section = StorefrontSection.Create(_tenantId, _pageId, "Hero");
        var config = ComponentConfig.CreateButtonBlock("Click", "https://example.com");
        var component = StorefrontComponent.Create(_tenantId, section.Id, ComponentType.ButtonBlock, config);

        // Act
        section.AddComponent(component);

        // Assert
        Assert.Single(section.Components);
        Assert.Contains(component, section.Components);
    }

    [Fact]
    public void AddComponent_WithNullComponent_ThrowsArgumentNullException()
    {
        // Arrange
        var section = StorefrontSection.Create(_tenantId, _pageId, "Hero");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => section.AddComponent(null));
    }

    [Fact]
    public void AddComponent_WithDifferentTenant_ThrowsInvalidOperationException()
    {
        // Arrange
        var section = StorefrontSection.Create(_tenantId, _pageId, "Hero");
        var differentTenant = Guid.NewGuid();
        var config = ComponentConfig.CreateButtonBlock("Click", "https://example.com");
        var component = StorefrontComponent.Create(differentTenant, section.Id, ComponentType.ButtonBlock, config);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => section.AddComponent(component));
        Assert.Contains("same tenant", ex.Message.ToLower());
    }

    [Fact]
    public void AddComponent_WithWrongSectionId_ThrowsInvalidOperationException()
    {
        // Arrange
        var section = StorefrontSection.Create(_tenantId, _pageId, "Hero");
        var wrongSectionId = Guid.NewGuid();
        var config = ComponentConfig.CreateButtonBlock("Click", "https://example.com");
        var component = StorefrontComponent.Create(_tenantId, wrongSectionId, ComponentType.ButtonBlock, config);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => section.AddComponent(component));
        Assert.Contains("component section id must match", ex.Message.ToLower());
    }

    [Fact]
    public void RemoveComponent_WithValidComponentId_RemovesFromCollection()
    {
        // Arrange
        var section = StorefrontSection.Create(_tenantId, _pageId, "Hero");
        var config = ComponentConfig.CreateButtonBlock("Click", "https://example.com");
        var component = StorefrontComponent.Create(_tenantId, section.Id, ComponentType.ButtonBlock, config);
        section.AddComponent(component);

        // Act
        section.RemoveComponent(component.Id);

        // Assert
        Assert.Empty(section.Components);
    }

    [Fact]
    public void RemoveComponent_WithNonExistentComponentId_DoesNotThrow()
    {
        // Arrange
        var section = StorefrontSection.Create(_tenantId, _pageId, "Hero");

        // Act & Assert (should not throw)
        section.RemoveComponent(Guid.NewGuid());
        Assert.Empty(section.Components);
    }

    [Fact]
    public void GetComponent_WithValidComponentId_ReturnsComponent()
    {
        // Arrange
        var section = StorefrontSection.Create(_tenantId, _pageId, "Hero");
        var config = ComponentConfig.CreateButtonBlock("Click", "https://example.com");
        var component = StorefrontComponent.Create(_tenantId, section.Id, ComponentType.ButtonBlock, config);
        section.AddComponent(component);

        // Act
        var retrievedComponent = section.GetComponent(component.Id);

        // Assert
        Assert.NotNull(retrievedComponent);
        Assert.Equal(component.Id, retrievedComponent.Id);
    }

    [Fact]
    public void GetComponent_WithNonExistentComponentId_ReturnsNull()
    {
        // Arrange
        var section = StorefrontSection.Create(_tenantId, _pageId, "Hero");

        // Act
        var retrievedComponent = section.GetComponent(Guid.NewGuid());

        // Assert
        Assert.Null(retrievedComponent);
    }

    [Fact]
    public void GetVisibleComponents_FiltersHiddenComponents()
    {
        // Arrange
        var section = StorefrontSection.Create(_tenantId, _pageId, "Hero");
        var config1 = ComponentConfig.CreateButtonBlock("Click", "https://example.com");
        var config2 = ComponentConfig.CreateImageBlock("https://example.com/image.jpg");
        var component1 = StorefrontComponent.Create(_tenantId, section.Id, ComponentType.ButtonBlock, config1);
        var component2 = StorefrontComponent.Create(_tenantId, section.Id, ComponentType.ImageBlock, config2);
        component2.Hide();
        
        section.AddComponent(component1);
        section.AddComponent(component2);

        // Act
        var visibleComponents = section.GetVisibleComponents().ToList();

        // Assert
        Assert.Single(visibleComponents);
        Assert.Equal(component1.Id, visibleComponents[0].Id);
    }

    [Fact]
    public void GetVisibleComponents_ReturnsOrderedByDisplayOrder()
    {
        // Arrange
        var section = StorefrontSection.Create(_tenantId, _pageId, "Hero");
        var config1 = ComponentConfig.CreateButtonBlock("Click", "https://example.com");
        var config2 = ComponentConfig.CreateImageBlock("https://example.com/image.jpg");
        var component1 = StorefrontComponent.Create(_tenantId, section.Id, ComponentType.ButtonBlock, config1);
        component1.SetDisplayOrder(2);
        
        var component2 = StorefrontComponent.Create(_tenantId, section.Id, ComponentType.ImageBlock, config2);
        component2.SetDisplayOrder(1);
        
        section.AddComponent(component1);
        section.AddComponent(component2);

        // Act
        var visibleComponents = section.GetVisibleComponents().ToList();

        // Assert
        Assert.Equal(2, visibleComponents.Count);
        Assert.Equal(component2.Id, visibleComponents[0].Id); // component2 has lower displayOrder
        Assert.Equal(component1.Id, visibleComponents[1].Id);
    }

    [Fact]
    public void ReorderComponents_UpdatesDisplayOrder()
    {
        // Arrange
        var section = StorefrontSection.Create(_tenantId, _pageId, "Hero");
        var config1 = ComponentConfig.CreateButtonBlock("Click", "https://example.com");
        var config2 = ComponentConfig.CreateImageBlock("https://example.com/image.jpg");
        var component1 = StorefrontComponent.Create(_tenantId, section.Id, ComponentType.ButtonBlock, config1);
        var component2 = StorefrontComponent.Create(_tenantId, section.Id, ComponentType.ImageBlock, config2);
        
        section.AddComponent(component1);
        section.AddComponent(component2);

        var newOrders = new Dictionary<Guid, int>
        {
            { component1.Id, 5 },
            { component2.Id, 3 }
        };

        // Act
        section.ReorderComponents(newOrders);

        // Assert
        Assert.Equal(5, component1.DisplayOrder);
        Assert.Equal(3, component2.DisplayOrder);
    }

    [Fact]
    public void ReorderComponents_WithNullDictionary_ThrowsArgumentNullException()
    {
        // Arrange
        var section = StorefrontSection.Create(_tenantId, _pageId, "Hero");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => section.ReorderComponents(null));
    }

    #endregion

    #region Update and Display Order Tests

    [Fact]
    public void Update_WithValidData_UpdatesAllFields()
    {
        // Arrange
        var section = StorefrontSection.Create(_tenantId, _pageId, "Hero");

        // Act
        section.Update(
            name: "Updated Hero",
            description: "Updated description",
            cssClass: "hero-large",
            backgroundColor: "#FF0000",
            backgroundImageUrl: "https://example.com/image.jpg"
        );

        // Assert
        Assert.Equal("Updated Hero", section.Name);
        Assert.Equal("Updated description", section.Description);
        Assert.Equal("hero-large", section.CssClass);
        Assert.Equal("#FF0000", section.BackgroundColor);
        Assert.Equal("https://example.com/image.jpg", section.BackgroundImageUrl);
    }

    [Fact]
    public void SetDisplayOrder_WithValidOrder_UpdatesOrder()
    {
        // Arrange
        var section = StorefrontSection.Create(_tenantId, _pageId, "Hero");

        // Act
        section.SetDisplayOrder(5);

        // Assert
        Assert.Equal(5, section.DisplayOrder);
    }

    [Fact]
    public void SetDisplayOrder_WithNegativeOrder_ThrowsArgumentException()
    {
        // Arrange
        var section = StorefrontSection.Create(_tenantId, _pageId, "Hero");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => section.SetDisplayOrder(-1));
        Assert.Contains("negative", ex.Message.ToLower());
    }

    #endregion
}
