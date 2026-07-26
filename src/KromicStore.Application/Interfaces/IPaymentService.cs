namespace KromicStore.Application.Interfaces;

using Domain.ValueObjects;

/// <summary>
/// Interface for payment processing.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Initiates a payment transaction.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <param name="amount">The payment amount.</param>
    /// <param name="method">The payment method (e.g., "razorpay", "stripe").</param>
    /// <returns>Payment gateway response with transaction details.</returns>
    Task<PaymentInitiationResponse> InitiatePaymentAsync(
        Guid orderId,
        Money amount,
        string method,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a payment transaction.
    /// </summary>
    Task<PaymentVerificationResponse> VerifyPaymentAsync(
        string transactionId,
        string signature,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a refund.
    /// </summary>
    Task<RefundResponse> RefundAsync(
        string transactionId,
        Money amount,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Payment initiation response.
/// </summary>
public record PaymentInitiationResponse(
    string TransactionId,
    string PaymentUrl,
    DateTime ExpiresAt);

/// <summary>
/// Payment verification response.
/// </summary>
public record PaymentVerificationResponse(
    bool IsSuccessful,
    string TransactionId,
    Money Amount,
    DateTime ProcessedAt);

/// <summary>
/// Refund response.
/// </summary>
public record RefundResponse(
    bool IsSuccessful,
    string RefundId,
    Money Amount,
    DateTime ProcessedAt);
