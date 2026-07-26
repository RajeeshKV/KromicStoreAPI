#nullable disable

using Xunit;
using KromicStore.Domain.Entities;

namespace KromicStore.Tests.Unit.Domain;

/// <summary>
/// Unit tests for StorefrontPage entity validating page lifecycle and section management.
/// Tests: creation, visibility states, slug normalization, and section management.
/// </summary>
public class StorefrontPageTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _storefrontId = Guid.NewGuid();

    #region Creation Tests

    [Fact]
    public void Create_WithValidInputs_ReturnsPageEntity()
    {
        // Arrange
        var name = "Home";
        var slug = "home";

        // Act
        var page = StorefrontPage.Create(_tenantId, _storefrontId, name, slug);

        // Assert
        Assert.NotNull(page);
        Assert.Equal(_tenantId, page.TenantId);
        Assert.Equal(_storefrontId, page.StorefrontId);
        Assert.Equal(name, page.Name);
        Assert.Equal(slug, page.Slug);
        Assert.Equal(PageVisibility.Draft, page.Visibility);
        Assert.NotEqual(Guid.Empty, page.Id);
        Assert.Empty(page.Sections);
    }

    [Fact]
    public void Create_WithSpacesInSlug_NormalizesSlug()
    {
        // Arrange & Act
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home Page", "home page");

        // Assert
        Assert.Equal("home-page", page.Slug);
    }

    [Fact]
    public void Create_WithUppercaseSlug_ConvertsToLowercase()
    {
        // Arrange & Act
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "HOME");

        // Assert
        Assert.Equal("home", page.Slug);
    }

    [Fact]
    public void Create_WithEmptyTenantId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            StorefrontPage.Create(Guid.Empty, _storefrontId, "Home", "home"));
        Assert.Contains("Tenant ID", ex.Message);
    }

    [Fact]
    public void Create_WithEmptyStorefrontId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            StorefrontPage.Create(_tenantId, Guid.Empty, "Home", "home"));
        Assert.Contains("Storefront ID", ex.Message);
    }

    [Fact]
    public void Create_WithNullName_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            StorefrontPage.Create(_tenantId, _storefrontId, null, "home"));
        Assert.Contains("name", ex.Message.ToLower());
    }

    [Fact]
    public void Create_WithNullSlug_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            StorefrontPage.Create(_tenantId, _storefrontId, "Home", null));
        Assert.Contains("slug", ex.Message.ToLower());
    }

    #endregion

    #region Visibility Tests

    [Fact]
    public void Publish_WhenDraft_ChangesVisibilityToPublished()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");

        // Act
        page.Publish();

        // Assert
        Assert.Equal(PageVisibility.Published, page.Visibility);
    }

    [Fact]
    public void Publish_WhenAlreadyPublished_ThrowsInvalidOperationException()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");
        page.Publish();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => page.Publish());
        Assert.Contains("already published", ex.Message.ToLower());
    }

    [Fact]
    public void Unpublish_WhenPublished_ChangesVisibilityToDraft()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");
        page.Publish();

        // Act
        page.Unpublish();

        // Assert
        Assert.Equal(PageVisibility.Draft, page.Visibility);
    }

    [Fact]
    public void Unpublish_WhenNotPublished_ThrowsInvalidOperationException()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => page.Unpublish());
        Assert.Contains("only published", ex.Message.ToLower());
    }

    [Fact]
    public void Archive_WhenNotArchived_ChangesVisibilityToArchived()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");

        // Act
        page.Archive();

        // Assert
        Assert.Equal(PageVisibility.Archived, page.Visibility);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ThrowsInvalidOperationException()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");
        page.Archive();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => page.Archive());
        Assert.Contains("already archived", ex.Message.ToLower());
    }

    #endregion

    #region Section Management Tests

    [Fact]
    public void AddSection_WithValidSection_AddsToCollection()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");
        var section = StorefrontSection.Create(_tenantId, page.Id, "Hero");

        // Act
        page.AddSection(section);

        // Assert
        Assert.Single(page.Sections);
        Assert.Contains(section, page.Sections);
    }

    [Fact]
    public void AddSection_WithNullSection_ThrowsArgumentNullException()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => page.AddSection(null));
    }

    [Fact]
    public void AddSection_WithDifferentTenant_ThrowsInvalidOperationException()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");
        var differentTenant = Guid.NewGuid();
        var section = StorefrontSection.Create(differentTenant, page.Id, "Hero");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => page.AddSection(section));
        Assert.Contains("same tenant", ex.Message.ToLower());
    }

    [Fact]
    public void AddSection_WithWrongPageId_ThrowsInvalidOperationException()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");
        var wrongPageId = Guid.NewGuid();
        var section = StorefrontSection.Create(_tenantId, wrongPageId, "Hero");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => page.AddSection(section));
        Assert.Contains("section page id must match", ex.Message.ToLower());
    }

    [Fact]
    public void RemoveSection_WithValidSectionId_RemovesFromCollection()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");
        var section = StorefrontSection.Create(_tenantId, page.Id, "Hero");
        page.AddSection(section);

        // Act
        page.RemoveSection(section.Id);

        // Assert
        Assert.Empty(page.Sections);
    }

    [Fact]
    public void RemoveSection_WithNonExistentSectionId_DoesNotThrow()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");

        // Act & Assert (should not throw)
        page.RemoveSection(Guid.NewGuid());
        Assert.Empty(page.Sections);
    }

    [Fact]
    public void GetSection_WithValidSectionId_ReturnsSection()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");
        var section = StorefrontSection.Create(_tenantId, page.Id, "Hero");
        page.AddSection(section);

        // Act
        var retrievedSection = page.GetSection(section.Id);

        // Assert
        Assert.NotNull(retrievedSection);
        Assert.Equal(section.Id, retrievedSection.Id);
    }

    [Fact]
    public void GetSection_WithNonExistentSectionId_ReturnsNull()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");

        // Act
        var retrievedSection = page.GetSection(Guid.NewGuid());

        // Assert
        Assert.Null(retrievedSection);
    }

    [Fact]
    public void GetVisibleSections_FiltersHiddenSections()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");
        var section1 = StorefrontSection.Create(_tenantId, page.Id, "Hero");
        var section2 = StorefrontSection.Create(_tenantId, page.Id, "Features");
        section2.Hide(); // Hide this section
        
        page.AddSection(section1);
        page.AddSection(section2);

        // Act
        var visibleSections = page.GetVisibleSections().ToList();

        // Assert
        Assert.Single(visibleSections);
        Assert.Equal(section1.Id, visibleSections[0].Id);
    }

    [Fact]
    public void GetVisibleSections_ReturnsOrderedByDisplayOrder()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");
        var section1 = StorefrontSection.Create(_tenantId, page.Id, "Hero");
        section1.SetDisplayOrder(2);
        
        var section2 = StorefrontSection.Create(_tenantId, page.Id, "Features");
        section2.SetDisplayOrder(1);
        
        page.AddSection(section1);
        page.AddSection(section2);

        // Act
        var visibleSections = page.GetVisibleSections().ToList();

        // Assert
        Assert.Equal(2, visibleSections.Count);
        Assert.Equal(section2.Id, visibleSections[0].Id); // section2 has lower displayOrder
        Assert.Equal(section1.Id, visibleSections[1].Id);
    }

    [Fact]
    public void ReorderSections_UpdatesDisplayOrder()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");
        var section1 = StorefrontSection.Create(_tenantId, page.Id, "Hero");
        var section2 = StorefrontSection.Create(_tenantId, page.Id, "Features");
        
        page.AddSection(section1);
        page.AddSection(section2);

        var newOrders = new Dictionary<Guid, int>
        {
            { section1.Id, 5 },
            { section2.Id, 3 }
        };

        // Act
        page.ReorderSections(newOrders);

        // Assert
        Assert.Equal(5, section1.DisplayOrder);
        Assert.Equal(3, section2.DisplayOrder);
    }

    [Fact]
    public void ReorderSections_WithNullDictionary_ThrowsArgumentNullException()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => page.ReorderSections(null));
    }

    #endregion

    #region Update and Featured Tests

    [Fact]
    public void Update_WithValidData_UpdatesAllFields()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");

        // Act
        page.Update(
            name: "Homepage",
            slug: "home-page",
            description: "Main landing page",
            layoutType: "hero",
            metaKeywords: "home, landing"
        );

        // Assert
        Assert.Equal("Homepage", page.Name);
        Assert.Equal("home-page", page.Slug);
        Assert.Equal("Main landing page", page.Description);
        Assert.Equal("hero", page.LayoutType);
        Assert.Equal("home, landing", page.MetaKeywords);
    }

    [Fact]
    public void SetDisplayOrder_WithValidOrder_UpdatesOrder()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");

        // Act
        page.SetDisplayOrder(5);

        // Assert
        Assert.Equal(5, page.DisplayOrder);
    }

    [Fact]
    public void SetDisplayOrder_WithNegativeOrder_ThrowsArgumentException()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => page.SetDisplayOrder(-1));
        Assert.Contains("negative", ex.Message.ToLower());
    }

    [Fact]
    public void SetFeatured_MarksPageAsFeatured()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");

        // Act
        page.SetFeatured(true);

        // Assert
        Assert.True(page.IsFeatured);
    }

    [Fact]
    public void SetFeatured_WithFalse_UnmarksPageAsFeatured()
    {
        // Arrange
        var page = StorefrontPage.Create(_tenantId, _storefrontId, "Home", "home");
        page.SetFeatured(true);

        // Act
        page.SetFeatured(false);

        // Assert
        Assert.False(page.IsFeatured);
    }

    #endregion
}
