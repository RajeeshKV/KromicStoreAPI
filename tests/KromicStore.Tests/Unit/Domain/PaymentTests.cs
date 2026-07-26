#nullable disable

using Xunit;
using KromicStore.Domain.Entities;
using KromicStore.Domain.Enums;
using KromicStore.Domain.ValueObjects;

namespace KromicStore.Tests.Unit.Domain;

public class PaymentTests
{
    private static readonly Guid ValidTenantId = Guid.NewGuid();
    private static readonly Guid ValidOrderId = Guid.NewGuid();
    private static readonly Money ValidAmount = new Money(500m);

    // ─── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void CreatePayment_WithValidData_ShouldHavePendingStatus()
    {
        // Act
        var payment = Payment.Create(ValidTenantId, ValidOrderId, ValidAmount);

        // Assert
        Assert.Equal(ValidTenantId, payment.TenantId);
        Assert.Equal(ValidOrderId, payment.OrderId);
        Assert.Equal(500m, payment.Amount.Amount);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Fact]
    public void CreatePayment_WithZeroAmount_ShouldThrowArgumentException()
    {
        // Arrange
        var zeroAmount = new Money(0m);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Payment.Create(ValidTenantId, ValidOrderId, zeroAmount));
    }

    [Fact]
    public void CreatePayment_WithEmptyTenantId_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Payment.Create(Guid.Empty, ValidOrderId, ValidAmount));
    }

    [Fact]
    public void CreatePayment_WithEmptyOrderId_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Payment.Create(ValidTenantId, Guid.Empty, ValidAmount));
    }

    // ─── MarkAsProcessed ──────────────────────────────────────────────────────

    [Fact]
    public void MarkAsProcessed_WithValidExternalId_ShouldSetStatusToCompleted()
    {
        // Arrange
        var payment = Payment.Create(ValidTenantId, ValidOrderId, ValidAmount);

        // Act
        payment.MarkAsProcessed("EXT-PAY-001", "UPI");

        // Assert
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal("EXT-PAY-001", payment.ExternalPaymentId);
        Assert.Equal("UPI", payment.PaymentMethod);
        Assert.NotNull(payment.PaidAt);
    }

    [Fact]
    public void MarkAsProcessed_ShouldRecordATransaction()
    {
        // Arrange
        var payment = Payment.Create(ValidTenantId, ValidOrderId, ValidAmount);

        // Act
        payment.MarkAsProcessed("EXT-PAY-001");

        // Assert
        Assert.Single(payment.Transactions);
    }

    [Fact]
    public void MarkAsProcessed_WithEmptyExternalId_ShouldThrowArgumentException()
    {
        // Arrange
        var payment = Payment.Create(ValidTenantId, ValidOrderId, ValidAmount);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => payment.MarkAsProcessed(""));
    }

    // ─── MarkAsFailed ─────────────────────────────────────────────────────────

    [Fact]
    public void MarkAsFailed_WithReason_ShouldSetStatusToFailed()
    {
        // Arrange
        var payment = Payment.Create(ValidTenantId, ValidOrderId, ValidAmount);

        // Act
        payment.MarkAsFailed("Insufficient funds");

        // Assert
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("Insufficient funds", payment.FailureReason);
    }

    [Fact]
    public void MarkAsFailed_WhenAlreadyFailed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payment = Payment.Create(ValidTenantId, ValidOrderId, ValidAmount);
        payment.MarkAsFailed("Network error");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => payment.MarkAsFailed("Another reason"));
    }

    [Fact]
    public void MarkAsFailed_WithEmptyReason_ShouldThrowArgumentException()
    {
        // Arrange
        var payment = Payment.Create(ValidTenantId, ValidOrderId, ValidAmount);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => payment.MarkAsFailed(""));
    }
}
