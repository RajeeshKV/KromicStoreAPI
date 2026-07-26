namespace KromicStore.Tests.Unit.Domain.Entities;

using KromicStore.Domain.Entities;
using KromicStore.Domain.Enums;
using KromicStore.Domain.ValueObjects;
using Xunit;

/// <summary>
/// Unit tests for the Product entity.
/// </summary>
public class ProductTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var price = new Money(1000);

        // Act
        var product = Product.Create(
            _tenantId,
            "SKU-001",
            "Test Product",
            "A test product",
            price,
            100);

        // Assert
        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("SKU-001", product.Sku);
        Assert.Equal("Test Product", product.Name);
        Assert.Equal(ProductStatus.Draft, product.Status);
        Assert.Equal(100, product.StockQuantity);
    }

    [Fact]
    public void Create_WithoutTenantId_ShouldThrow()
    {
        // Arrange
        var price = new Money(1000);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Product.Create(
            Guid.Empty,
            "SKU-001",
            "Test Product",
            "A test product",
            price,
            100));
    }

    [Fact]
    public void Publish_FromDraft_ShouldSucceed()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.Publish();

        // Assert
        Assert.Equal(ProductStatus.Active, product.Status);
    }

    [Fact]
    public void ReduceStock_WithValidQuantity_ShouldSucceed()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.ReduceStock(50);

        // Assert
        Assert.Equal(50, product.StockQuantity);
    }

    [Fact]
    public void ReduceStock_WithInsufficientStock_ShouldThrow()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => product.ReduceStock(150));
    }

    [Fact]
    public void Archive_ShouldUpdateStatus()
    {
        // Arrange
        var product = CreateTestProduct();
        product.Publish();

        // Act
        product.Archive();

        // Assert
        Assert.Equal(ProductStatus.Archived, product.Status);
    }

    private Product CreateTestProduct()
    {
        var price = new Money(1000);
        return Product.Create(
            _tenantId,
            "SKU-001",
            "Test Product",
            "A test product",
            price,
            100);
    }
}
