#nullable disable

using Xunit;
using KromicStore.Domain.Entities;
using KromicStore.Domain.ValueObjects;

namespace KromicStore.Tests.Unit.Domain;

public class CustomerTests
{
    private static readonly Guid ValidTenantId = Guid.NewGuid();

    // ─── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void CreateCustomer_WithValidData_ShouldSucceed()
    {
        // Act
        var customer = Customer.Create(ValidTenantId, "Alice", "Smith", "alice@example.com");

        // Assert
        Assert.Equal(ValidTenantId, customer.TenantId);
        Assert.Equal("Alice", customer.FirstName);
        Assert.Equal("Smith", customer.LastName);
        Assert.Equal("alice@example.com", customer.Email);
        Assert.True(customer.IsActive);
        Assert.Equal(0m, customer.LifetimeValue.Amount);
        Assert.Equal(0, customer.TotalOrdersCount);
    }

    [Fact]
    public void CreateCustomer_WithEmptyTenantId_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Customer.Create(Guid.Empty, "Alice", "Smith", "alice@example.com"));
    }

    [Fact]
    public void CreateCustomer_WithEmptyFirstName_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Customer.Create(ValidTenantId, "", "Smith", "alice@example.com"));
    }

    [Fact]
    public void CreateCustomer_WithEmptyLastName_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Customer.Create(ValidTenantId, "Alice", "  ", "alice@example.com"));
    }

    [Fact]
    public void CreateCustomer_WithEmptyEmail_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Customer.Create(ValidTenantId, "Alice", "Smith", ""));
    }

    // ─── GetFullName ──────────────────────────────────────────────────────────

    [Fact]
    public void GetFullName_ShouldReturnFirstNameSpaceLastName()
    {
        // Arrange
        var customer = Customer.Create(ValidTenantId, "Alice", "Smith", "alice@example.com");

        // Act
        var fullName = customer.GetFullName();

        // Assert
        Assert.Equal("Alice Smith", fullName);
    }

    // ─── RecordNewOrder / LifetimeValue ───────────────────────────────────────

    [Fact]
    public void RecordNewOrder_ShouldIncreaseLifetimeValueAndOrderCount()
    {
        // Arrange
        var customer = Customer.Create(ValidTenantId, "Alice", "Smith", "alice@example.com");

        // Act
        customer.RecordNewOrder(new Money(100m));

        // Assert
        Assert.Equal(100m, customer.LifetimeValue.Amount);
        Assert.Equal(1, customer.TotalOrdersCount);
        Assert.NotNull(customer.LastOrderAt);
    }

    [Fact]
    public void RecordNewOrder_MultipleTimes_ShouldAccumulateLifetimeValue()
    {
        // Arrange
        var customer = Customer.Create(ValidTenantId, "Alice", "Smith", "alice@example.com");

        // Act
        customer.RecordNewOrder(new Money(100m));
        customer.RecordNewOrder(new Money(250m));
        customer.RecordNewOrder(new Money(75m));

        // Assert
        Assert.Equal(425m, customer.LifetimeValue.Amount);
        Assert.Equal(3, customer.TotalOrdersCount);
    }

    // ─── RecordPurchase (alternative accumulator) ─────────────────────────────

    [Fact]
    public void RecordPurchase_ShouldIncreaseLifetimeValueAndOrderCount()
    {
        // Arrange
        var customer = Customer.Create(ValidTenantId, "Bob", "Jones", "bob@example.com");

        // Act
        customer.RecordPurchase(new Money(200m));

        // Assert
        Assert.Equal(200m, customer.LifetimeValue.Amount);
        Assert.Equal(1, customer.TotalOrdersCount);
    }

    [Fact]
    public void RecordPurchase_MultipleCalls_ShouldAccumulateCorrectly()
    {
        // Arrange
        var customer = Customer.Create(ValidTenantId, "Bob", "Jones", "bob@example.com");

        // Act
        customer.RecordPurchase(new Money(100m));
        customer.RecordPurchase(new Money(100m));

        // Assert
        Assert.Equal(200m, customer.LifetimeValue.Amount);
        Assert.Equal(2, customer.TotalOrdersCount);
    }

    // ─── Deactivate / Activate ────────────────────────────────────────────────

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var customer = Customer.Create(ValidTenantId, "Alice", "Smith", "alice@example.com");

        // Act
        customer.Deactivate();

        // Assert
        Assert.False(customer.IsActive);
    }

    [Fact]
    public void Activate_AfterDeactivate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var customer = Customer.Create(ValidTenantId, "Alice", "Smith", "alice@example.com");
        customer.Deactivate();

        // Act
        customer.Activate();

        // Assert
        Assert.True(customer.IsActive);
    }
}
