namespace KromicStore.Tests.Unit.Domain.Entities;

using KromicStore.Domain.Entities;
using KromicStore.Domain.Enums;
using KromicStore.Domain.ValueObjects;
using Xunit;

/// <summary>
/// Unit tests for the Order entity.
/// </summary>
public class OrderTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _customerId = Guid.NewGuid();

    private Address CreateTestAddress()
    {
        return new Address(
            "123 Main St",
            "New York",
            "NY",
            "10001",
            "USA");
    }

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var address = CreateTestAddress();

        // Act
        var order = Order.Create(_tenantId, _customerId, address, address);

        // Assert
        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.NotEmpty(order.OrderNumber);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
    }

    [Fact]
    public void AddItem_WithValidData_ShouldSucceed()
    {
        // Arrange
        var order = CreateTestOrder();
        var productId = Guid.NewGuid();
        var unitPrice = new Money(100);

        // Act
        order.AddItem(productId, 5, unitPrice);

        // Assert
        Assert.Single(order.Items);
        Assert.Equal(5, order.Items.First().Quantity);
    }

    [Fact]
    public void Confirm_FromPending_ShouldSucceed()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        order.Confirm();

        // Assert
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void RecordPayment_ShouldUpdateStatus()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Confirm();

        // Act
        order.RecordPayment("razorpay", "txn-123");

        // Assert
        Assert.Equal(PaymentStatus.Completed, order.PaymentStatus);
        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Fact]
    public void MarkAsProcessing_FromPaid_ShouldSucceed()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Confirm();
        order.RecordPayment("razorpay");

        // Act
        order.MarkAsProcessing();

        // Assert
        Assert.Equal(OrderStatus.Processing, order.Status);
    }

    [Fact]
    public void Cancel_ShouldUpdateStatus()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        order.Cancel();

        // Assert
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    private Order CreateTestOrder()
    {
        var address = CreateTestAddress();
        return Order.Create(_tenantId, _customerId, address, address);
    }
}
