namespace KromicStore.Application.Interfaces;

/// <summary>
/// Interface for order payment processing and management.
/// </summary>
public interface IOrderPaymentService
{
    /// <summary>
    /// Initiates an order payment with the payment gateway.
    /// </summary>
    /// <param name="orderId">The order ID to process payment for.</param>
    /// <param name="amountInRupees">The payment amount in Indian Rupees.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Service result containing payment initiation details including Razorpay Order ID and payment link.</returns>
    Task<ServiceResult<OrderPaymentInitiationResponse>> InitiateOrderPaymentAsync(
        Guid orderId,
        decimal amountInRupees,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures a payment after successful authorization.
    /// </summary>
    /// <param name="razorpayPaymentId">The Razorpay payment ID.</param>
    /// <param name="razorpayOrderId">The Razorpay order ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Service result indicating whether the payment capture was successful.</returns>
    Task<ServiceResult<bool>> CapturePaymentAsync(
        string razorpayPaymentId,
        string razorpayOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles payment failure scenarios and updates order status accordingly.
    /// </summary>
    /// <param name="razorpayOrderId">The Razorpay order ID that failed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Service result indicating whether the failure was handled successfully.</returns>
    Task<ServiceResult<bool>> HandlePaymentFailureAsync(
        string razorpayOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a refund for a paid order.
    /// </summary>
    /// <param name="razorpayPaymentId">The Razorpay payment ID to refund.</param>
    /// <param name="amountInRupees">The refund amount in Indian Rupees.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Service result indicating whether the refund was processed successfully.</returns>
    Task<ServiceResult<bool>> ProcessRefundAsync(
        string razorpayPaymentId,
        decimal amountInRupees,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Response from order payment initiation.
/// </summary>
public record OrderPaymentInitiationResponse(
    string RazorpayOrderId,
    decimal Amount,
    string Currency,
    string PaymentLink);
