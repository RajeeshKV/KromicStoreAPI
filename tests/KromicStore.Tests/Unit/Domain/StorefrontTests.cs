#nullable disable

using Xunit;
using KromicStore.Domain.Entities;
using KromicStore.Domain.ValueObjects;
using KromicStore.Domain.Enums;

namespace KromicStore.Tests.Unit.Domain;

/// <summary>
/// Unit tests for Storefront entity validating business logic and domain methods.
/// Tests: creation, status transitions, page management, and mandatory field tracking.
/// </summary>
public class StorefrontTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    #region Creation Tests

    [Fact]
    public void CreateFromScratch_WithValidInputs_ReturnsStorefrontEntity()
    {
        // Arrange
        var name = "My Store";

        // Act
        var storefront = Storefront.CreateFromScratch(_tenantId, name);

        // Assert
        Assert.NotNull(storefront);
        Assert.Equal(_tenantId, storefront.TenantId);
        Assert.Equal(name, storefront.Name);
        Assert.Equal(StorefrontStatus.Draft, storefront.Status);
        Assert.Null(storefront.ThemeId);
        Assert.NotEqual(Guid.Empty, storefront.Id);
        Assert.NotNull(storefront.MandatoryFields);
        Assert.Empty(storefront.Pages);
    }

    [Fact]
    public void CreateFromScratch_WithEmptyTenantId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            Storefront.CreateFromScratch(Guid.Empty, "My Store"));
        Assert.Contains("Tenant ID", ex.Message);
    }

    [Fact]
    public void CreateFromScratch_WithNullName_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            Storefront.CreateFromScratch(_tenantId, null));
        Assert.Contains("name", ex.Message.ToLower());
    }

    [Fact]
    public void CreateFromTheme_WithValidInputs_IncludesThemeId()
    {
        // Arrange
        var name = "Themed Store";
        var themeId = Guid.NewGuid();

        // Act
        var storefront = Storefront.CreateFromTheme(_tenantId, name, themeId);

        // Assert
        Assert.NotNull(storefront);
        Assert.Equal(themeId, storefront.ThemeId);
        Assert.Equal(_tenantId, storefront.TenantId);
        Assert.Equal(name, storefront.Name);
    }

    #endregion

    #region Status Transition Tests

    [Fact]
    public void Publish_WhenAllMandatoryFieldsProvided_ChangesStatusToPublished()
    {
        // Arrange
        var storefront = Storefront.CreateFromScratch(_tenantId, "Store");
        // Update mandatory fields to not be placeholders
        storefront.UpdateMandatoryFieldsStatus(
            storeNameProvided: true,
            logoProvided: true,
            emailProvided: true,
            phoneProvided: true,
            addressProvided: true,
            currencyProvided: true,
            countryProvided: true,
            brandColorProvided: true,
            copyrightProvided: true
        );

        // Act
        storefront.Publish();

        // Assert
        Assert.Equal(StorefrontStatus.Published, storefront.Status);
    }

    [Fact]
    public void Publish_WhenMandatoryFieldsMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var storefront = Storefront.CreateFromScratch(_tenantId, "Store");
        // Mandatory fields are still placeholders (CreateFromScratch sets all as placeholders)

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => storefront.Publish());
        Assert.Contains("incomplete mandatory fields", ex.Message.ToLower());
    }

    [Fact]
    public void Publish_WhenAlreadyPublished_ThrowsInvalidOperationException()
    {
        // Arrange
        var storefront = Storefront.CreateFromScratch(_tenantId, "Store");
        storefront.UpdateMandatoryFieldsStatus(
            storeNameProvided: true,
            logoProvided: true,
            emailProvided: true,
            phoneProvided: true,
            addressProvided: true,
            currencyProvided: true,
            countryProvided: true,
            brandColorProvided: true,
            copyrightProvided: true
        );
        storefront.Publish();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => storefront.Publish());
        Assert.Contains("already published", ex.Message.ToLower());
    }

    [Fact]
    public void Unpublish_WhenPublished_ChangesStatusToDraft()
    {
        // Arrange
        var storefront = Storefront.CreateFromScratch(_tenantId, "Store");
        storefront.UpdateMandatoryFieldsStatus(
            storeNameProvided: true,
            logoProvided: true,
            emailProvided: true,
            phoneProvided: true,
            addressProvided: true,
            currencyProvided: true,
            countryProvided: true,
            brandColorProvided: true,
            copyrightProvided: true
        );
        storefront.Publish();

        // Act
        storefront.Unpublish();

        // Assert
        Assert.Equal(StorefrontStatus.Draft, storefront.Status);
    }

    [Fact]
    public void Unpublish_WhenNotPublished_ThrowsInvalidOperationException()
    {
        // Arrange
        var storefront = Storefront.CreateFromScratch(_tenantId, "Store");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => storefront.Unpublish());
        Assert.Contains("only published", ex.Message.ToLower());
    }

    [Fact]
    public void Archive_WhenNotArchived_ChangesStatusToArchived()
    {
        // Arrange
        var storefront = Storefront.CreateFromScratch(_tenantId, "Store");

        // Act
        storefront.Archive();

        // Assert
        Assert.Equal(StorefrontStatus.Archived, storefront.Status);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ThrowsInvalidOperationException()
    {
        // Arrange
        var storefront = Storefront.CreateFromScratch(_tenantId, "Store");
        storefront.Archive();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => storefront.Archive());
        Assert.Contains("already archived", ex.Message.ToLower());
    }

    #endregion

    #region Page Management Tests

    [Fact]
    public void AddPage_WithValidPage_AddsToCollection()
    {
        // Arrange
        var storefront = Storefront.CreateFromScratch(_tenantId, "Store");
        var page = StorefrontPage.Create(_tenantId, storefront.Id, "Home", "home");

        // Act
        storefront.AddPage(page);

        // Assert
        Assert.Single(storefront.Pages);
        Assert.Contains(page, storefront.Pages);
    }

    [Fact]
    public void AddPage_WithNullPage_ThrowsArgumentNullException()
    {
        // Arrange
        var storefront = Storefront.CreateFromScratch(_tenantId, "Store");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => storefront.AddPage(null));
    }

    [Fact]
    public void AddPage_WithDifferentTenant_ThrowsInvalidOperationException()
    {
        // Arrange
        var storefront = Storefront.CreateFromScratch(_tenantId, "Store");
        var differentTenant = Guid.NewGuid();
        var page = StorefrontPage.Create(differentTenant, storefront.Id, "Home", "home");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => storefront.AddPage(page));
        Assert.Contains("same tenant", ex.Message.ToLower());
    }

    [Fact]
    public void RemovePage_WithValidPageId_RemovesFromCollection()
    {
        // Arrange
        var storefront = Storefront.CreateFromScratch(_tenantId, "Store");
        var page = StorefrontPage.Create(_tenantId, storefront.Id, "Home", "home");
        storefront.AddPage(page);

        // Act
        storefront.RemovePage(page.Id);

        // Assert
        Assert.Empty(storefront.Pages);
    }

    [Fact]
    public void RemovePage_WithNonExistentPageId_DoesNotThrow()
    {
        // Arrange
        var storefront = Storefront.CreateFromScratch(_tenantId, "Store");

        // Act & Assert (should not throw)
        storefront.RemovePage(Guid.NewGuid());
        Assert.Empty(storefront.Pages);
    }

    [Fact]
    public void GetPage_WithValidPageId_ReturnsPage()
    {
        // Arrange
        var storefront = Storefront.CreateFromScratch(_tenantId, "Store");
        var page = StorefrontPage.Create(_tenantId, storefront.Id, "Home", "home");
        storefront.AddPage(page);

        // Act
        var retrievedPage = storefront.GetPage(page.Id);

        // Assert
        Assert.NotNull(retrievedPage);
        Assert.Equal(page.Id, retrievedPage.Id);
    }

    [Fact]
    public void GetPage_WithNonExistentPageId_ReturnsNull()
    {
        // Arrange
        var storefront = Storefront.CreateFromScratch(_tenantId, "Store");

        // Act
        var retrievedPage = storefront.GetPage(Guid.NewGuid());

        // Assert
        Assert.Null(retrievedPage);
    }

    #endregion

    #region Mandatory Fields Tests

    [Fact]
    public void UpdateMandatoryFieldsStatus_WithAllFieldsProvided_TracksStatus()
    {
        // Arrange
        var storefront = Storefront.CreateFromScratch(_tenantId, "Store");

        // Act
        storefront.UpdateMandatoryFieldsStatus(
            storeNameProvided: true,
            logoProvided: true,
            emailProvided: true,
            phoneProvided: true,
            addressProvided: true,
            currencyProvided: true,
            countryProvided: true,
            brandColorProvided: true,
            copyrightProvided: true
        );

        // Assert
        Assert.True(storefront.MandatoryFields.AreAllFieldsProvided());
    }

    [Fact]
    public void UpdateMandatoryFieldsStatus_WithPartialFields_DoesNotMarkAllProvided()
    {
        // Arrange
        var storefront = Storefront.CreateFromScratch(_tenantId, "Store");

        // Act
        storefront.UpdateMandatoryFieldsStatus(
            storeNameProvided: true,
            logoProvided: true,
            emailProvided: false // Missing email
        );

        // Assert
        Assert.False(storefront.MandatoryFields.AreAllFieldsProvided());
    }

    #endregion

    #region Info Update Tests

    [Fact]
    public void UpdateInfo_WithValidData_UpdatesAllFields()
    {
        // Arrange
        var storefront = Storefront.CreateFromScratch(_tenantId, "Store");
        var newName = "Updated Store";
        var newEmail = "store@example.com";
        var newPhone = "1234567890";

        // Act
        storefront.UpdateInfo(
            name: newName,
            logoUrl: "https://example.com/logo.png",
            contactEmail: newEmail,
            contactPhone: newPhone,
            address: "123 Main St",
            currency: "USD",
            country: "US",
            brandColor: "#FF0000",
            copyright: "2024"
        );

        // Assert
        Assert.Equal(newName, storefront.Name);
        Assert.Equal(newEmail, storefront.ContactEmail);
        Assert.Equal(newPhone, storefront.ContactPhone);
        Assert.Equal("https://example.com/logo.png", storefront.LogoUrl);
        Assert.Equal("USD", storefront.Currency);
    }

    [Fact]
    public void UpdateInfo_WithPartialData_UpdatesProvidedFieldsOnly()
    {
        // Arrange
        var storefront = Storefront.CreateFromScratch(_tenantId, "Store");
        var originalEmail = storefront.ContactEmail;

        // Act
        storefront.UpdateInfo(
            name: "Updated Store",
            logoUrl: "https://example.com/logo.png"
            // Other fields not provided
        );

        // Assert
        Assert.Equal("Updated Store", storefront.Name);
        Assert.Equal("https://example.com/logo.png", storefront.LogoUrl);
        // Email should remain unchanged (or null)
    }

    #endregion
}
