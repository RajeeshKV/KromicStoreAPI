#nullable disable

using System.Text.RegularExpressions;
using Xunit;
using KromicStore.Domain.Entities;
using KromicStore.Domain.Enums;
using KromicStore.Domain.ValueObjects;

namespace KromicStore.Tests.Unit.Domain;

public class OrderTests
{
    private static readonly Guid ValidTenantId = Guid.NewGuid();
    private static readonly Guid ValidCustomerId = Guid.NewGuid();
    private static readonly Address ShippingAddr = new Address("123 Main St", "Mumbai", "MH", "400001", "IN");
    private static readonly Address BillingAddr = new Address("456 Park Ave", "Mumbai", "MH", "400002", "IN");

    private static Order CreateValidOrder() =>
        Order.Create(ValidTenantId, ValidCustomerId, ShippingAddr, BillingAddr);

    // ─── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void CreateOrder_WithValidData_ShouldSucceed()
    {
        // Act
        var order = CreateValidOrder();

        // Assert
        Assert.Equal(ValidTenantId, order.TenantId);
        Assert.Equal(ValidCustomerId, order.CustomerId);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
    }

    [Fact]
    public void CreateOrder_OrderNumber_ShouldMatchExpectedPattern()
    {
        // Act
        var order = CreateValidOrder();

        // Assert – format is ORD-{yyyyMMdd}-{8 hex chars uppercase}
        Assert.Matches(@"^ORD-\d{8}-[0-9A-F]{8}$", order.OrderNumber);
    }

    [Fact]
    public void CreateOrder_WithEmptyTenantId_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Order.Create(Guid.Empty, ValidCustomerId, ShippingAddr, BillingAddr));
    }

    [Fact]
    public void CreateOrder_WithEmptyCustomerId_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Order.Create(ValidTenantId, Guid.Empty, ShippingAddr, BillingAddr));
    }

    // ─── Status transitions ───────────────────────────────────────────────────

    [Fact]
    public void Confirm_FromPending_ShouldSetStatusToConfirmed()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act
        order.Confirm();

        // Assert
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void RecordPayment_FromConfirmed_ShouldSetStatusToPaid()
    {
        // Arrange
        var order = CreateValidOrder();
        order.Confirm();

        // Act
        order.RecordPayment("UPI", "GW-REF-001");

        // Assert
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(PaymentStatus.Completed, order.PaymentStatus);
    }

    [Fact]
    public void MarkAsShipped_FromProcessing_ShouldSetStatusToShipped()
    {
        // Arrange
        var order = CreateValidOrder();
        order.Confirm();
        order.RecordPayment("UPI");
        order.MarkAsProcessing();

        // Act
        order.MarkAsShipped("TRACK-9999");

        // Assert
        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.Equal("TRACK-9999", order.TrackingNumber);
    }

    [Fact]
    public void MarkAsDelivered_FromShipped_ShouldSetStatusToDelivered()
    {
        // Arrange
        var order = CreateValidOrder();
        order.Confirm();
        order.RecordPayment("UPI");
        order.MarkAsProcessing();
        order.MarkAsShipped("TRACK-9999");

        // Act
        order.MarkAsDelivered();

        // Assert
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void Cancel_FromPending_ShouldSetStatusToCancelled()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act
        order.Cancel();

        // Assert
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    // ─── Invalid transitions ──────────────────────────────────────────────────

    [Fact]
    public void MarkAsDelivered_FromPending_ShouldThrowInvalidOperationException()
    {
        // Arrange – order is still Pending
        var order = CreateValidOrder();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.MarkAsDelivered());
    }

    [Fact]
    public void Confirm_WhenNotPending_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var order = CreateValidOrder();
        order.Confirm(); // already Confirmed

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.Confirm());
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var order = CreateValidOrder();
        order.Cancel();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.Cancel());
    }

    // ─── Totals ───────────────────────────────────────────────────────────────

    [Fact]
    public void UpdateTotals_Total_ShouldEqualSubtotalPlusTaxPlusShipping()
    {
        // Arrange
        var order = CreateValidOrder();
        var subtotal = new Money(500m);
        var tax = new Money(90m);
        var shipping = new Money(50m);

        // Act
        order.UpdateTotals(subtotal, tax, shipping);

        // Assert
        Assert.Equal(640m, order.Total.Amount);
    }

    // ─── AddItem ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddItem_WithValidData_ShouldAppendToItems()
    {
        // Arrange
        var order = CreateValidOrder();
        var productId = Guid.NewGuid();

        // Act
        order.AddItem(productId, 2, new Money(100m));

        // Assert
        Assert.Single(order.Items);
        Assert.Equal(2, order.Items[0].Quantity);
    }

    [Fact]
    public void AddItem_WithZeroQuantity_ShouldThrowArgumentException()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            order.AddItem(Guid.NewGuid(), 0, new Money(100m)));
    }

    [Fact]
    public void AddItem_WithNegativeQuantity_ShouldThrowArgumentException()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            order.AddItem(Guid.NewGuid(), -1, new Money(100m)));
    }
}
