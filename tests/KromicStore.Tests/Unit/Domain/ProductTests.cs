#nullable disable

using Xunit;
using KromicStore.Domain.Entities;
using KromicStore.Domain.Enums;
using KromicStore.Domain.ValueObjects;

namespace KromicStore.Tests.Unit.Domain;

public class ProductTests
{
    private static readonly Guid ValidTenantId = Guid.NewGuid();
    private static readonly Money ValidPrice = new Money(99.99m);

    // ─── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void CreateProduct_WithValidData_ShouldSucceed()
    {
        // Arrange / Act
        var product = Product.Create(ValidTenantId, "SKU-001", "Test Product", "Description", ValidPrice, 50);

        // Assert
        Assert.Equal(ValidTenantId, product.TenantId);
        Assert.Equal("SKU-001", product.Sku);
        Assert.Equal("Test Product", product.Name);
        Assert.Equal(99.99m, product.Price.Amount);
        Assert.Equal(50, product.StockQuantity);
        Assert.Equal(ProductStatus.Draft, product.Status);
    }

    [Fact]
    public void CreateProduct_WithEmptyTenantId_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Product.Create(Guid.Empty, "SKU-001", "Product", "Desc", ValidPrice, 10));
    }

    [Fact]
    public void CreateProduct_WithEmptySku_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Product.Create(ValidTenantId, "", "Product", "Desc", ValidPrice, 10));
    }

    [Fact]
    public void CreateProduct_WithEmptyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Product.Create(ValidTenantId, "SKU-001", "   ", "Desc", ValidPrice, 10));
    }

    [Fact]
    public void CreateProduct_WithZeroPrice_ShouldThrowArgumentException()
    {
        // Arrange
        var zeroPrice = new Money(0m);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Product.Create(ValidTenantId, "SKU-001", "Product", "Desc", zeroPrice, 10));
    }

    [Fact]
    public void CreateProduct_WithNegativeStock_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Product.Create(ValidTenantId, "SKU-001", "Product", "Desc", ValidPrice, -1));
    }

    // ─── Publish ──────────────────────────────────────────────────────────────

    [Fact]
    public void Publish_WithPositiveStock_ShouldSetStatusToActive()
    {
        // Arrange
        var product = Product.Create(ValidTenantId, "SKU-001", "Product", "Desc", ValidPrice, 10);

        // Act
        product.Publish();

        // Assert
        Assert.Equal(ProductStatus.Active, product.Status);
    }

    [Fact]
    public void Publish_WithZeroStock_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var product = Product.Create(ValidTenantId, "SKU-001", "Product", "Desc", ValidPrice, 0);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => product.Publish());
    }

    [Fact]
    public void Publish_WhenAlreadyPublished_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var product = Product.Create(ValidTenantId, "SKU-001", "Product", "Desc", ValidPrice, 10);
        product.Publish();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => product.Publish());
    }

    // ─── Unpublish ────────────────────────────────────────────────────────────

    [Fact]
    public void Unpublish_WhenPublished_ShouldSetStatusToInactive()
    {
        // Arrange
        var product = Product.Create(ValidTenantId, "SKU-001", "Product", "Desc", ValidPrice, 10);
        product.Publish();

        // Act
        product.Unpublish();

        // Assert
        Assert.Equal(ProductStatus.Inactive, product.Status);
    }

    [Fact]
    public void Unpublish_WhenNotActive_ShouldThrowInvalidOperationException()
    {
        // Arrange – product is Draft (not Active)
        var product = Product.Create(ValidTenantId, "SKU-001", "Product", "Desc", ValidPrice, 10);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => product.Unpublish());
    }

    // ─── ReduceStock ──────────────────────────────────────────────────────────

    [Fact]
    public void ReduceStock_WithSufficientStock_ShouldDecreaseStockQuantity()
    {
        // Arrange
        var product = Product.Create(ValidTenantId, "SKU-001", "Product", "Desc", ValidPrice, 20);

        // Act
        product.ReduceStock(5);

        // Assert
        Assert.Equal(15, product.StockQuantity);
    }

    [Fact]
    public void ReduceStock_WithInsufficientStock_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var product = Product.Create(ValidTenantId, "SKU-001", "Product", "Desc", ValidPrice, 3);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => product.ReduceStock(5));
    }

    [Fact]
    public void ReduceStock_WithZeroQuantity_ShouldThrowArgumentException()
    {
        // Arrange
        var product = Product.Create(ValidTenantId, "SKU-001", "Product", "Desc", ValidPrice, 10);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => product.ReduceStock(0));
    }

    // ─── RestoreStock ─────────────────────────────────────────────────────────

    [Fact]
    public void RestoreStock_WithPositiveQuantity_ShouldIncreaseStockQuantity()
    {
        // Arrange
        var product = Product.Create(ValidTenantId, "SKU-001", "Product", "Desc", ValidPrice, 5);

        // Act
        product.RestoreStock(10);

        // Assert
        Assert.Equal(15, product.StockQuantity);
    }

    [Fact]
    public void RestoreStock_WithZeroQuantity_ShouldThrowArgumentException()
    {
        // Arrange
        var product = Product.Create(ValidTenantId, "SKU-001", "Product", "Desc", ValidPrice, 5);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => product.RestoreStock(0));
    }
}
