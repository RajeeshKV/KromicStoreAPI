namespace KromicStore.Tests.Unit.Domain.ValueObjects;

using KromicStore.Domain.ValueObjects;
using Xunit;

/// <summary>
/// Unit tests for the Money value object.
/// </summary>
public class MoneyTests
{
    [Fact]
    public void Create_WithValidAmount_ShouldSucceed()
    {
        // Arrange & Act
        var money = new Money(100, "INR");

        // Assert
        Assert.Equal(100, money.Amount);
        Assert.Equal("INR", money.Currency);
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldThrow()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentException>(() => new Money(-100));
    }

    [Fact]
    public void Create_WithoutCurrency_ShouldUseDefault()
    {
        // Arrange & Act
        var money = new Money(50);

        // Assert
        Assert.Equal(50, money.Amount);
        Assert.Equal("INR", money.Currency);
    }

    [Fact]
    public void ImplicitConversion_FromDecimal_ShouldWork()
    {
        // Arrange & Act
        Money money = 100.50m;

        // Assert
        Assert.Equal(100.50m, money.Amount);
    }

    [Fact]
    public void ImplicitConversion_ToDecimal_ShouldWork()
    {
        // Arrange
        var money = new Money(100.50m);

        // Act
        decimal amount = money;

        // Assert
        Assert.Equal(100.50m, amount);
    }
}
