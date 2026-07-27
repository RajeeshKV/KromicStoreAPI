namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using KromicStore.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Payments;

/// <summary>
/// Controller for managing payments and payment verification.
/// Handles payment creation, status verification, and refund requests.
/// </summary>
[ApiController]
[Route("api/v1/payments")]
[Produces("application/json")]
[Authorize(Policy = Permissions.BillingRead)]
public class PaymentController : BaseController
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    /// <summary>
    /// Initializes a new instance of the PaymentController class.
    /// </summary>
    /// <param name="tenantProvider">Provides tenant context information.</param>
    /// <param name="paymentService">Service for payment operations.</param>
    /// <param name="logger">Logger instance.</param>
    public PaymentController(
        ITenantProvider tenantProvider,
        IPaymentService paymentService,
        ILogger<PaymentController> logger)
        : base(tenantProvider)
    {
        _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initiates a payment for an order via Razorpay.
    /// Returns Razorpay order details for frontend integration.
    /// </summary>
    /// <param name="request">Payment creation request with order ID and optional idempotency key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Payment status with Razorpay order and payment IDs.</returns>
    /// <response code="200">Payment successfully initiated.</response>
    /// <response code="400">Invalid request or order validation failed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Order not found.</response>
    /// <response code="409">Duplicate payment attempt detected (idempotency).</response>
    /// <response code="503">Payment gateway temporarily unavailable.</response>
    [Authorize(Policy = Permissions.BillingWrite)]
    [HttpPost("create")]
    [ProducesResponseType(typeof(PaymentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreatePayment(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            if (request.OrderId == Guid.Empty)
                return BadRequest(new { error = "Valid order ID is required." });

            _logger.LogInformation(
                "Initiating payment for order {OrderId} in tenant {TenantId}",
                request.OrderId, CurrentTenantId);

            // Initiate payment through service
            var result = await _paymentService.InitiatePaymentAsync(
                request.OrderId,
                new Domain.ValueObjects.Money(0),  // Amount fetched from order in service
                "razorpay",
                cancellationToken);

            _logger.LogInformation(
                "Payment initiated successfully for order {OrderId}. Transaction ID: {TransactionId}",
                request.OrderId, result.TransactionId);

            // Return payment status with Razorpay details
            var response = new PaymentStatusResponse
            {
                PaymentId = Guid.Parse(result.TransactionId),
                RazorpayOrderId = result.TransactionId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Order {OrderId} not found for tenant {TenantId}", 
                request.OrderId, CurrentTenantId);
            return NotFound(new { error = "Order not found." });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Duplicate payment detected for order {OrderId}", request.OrderId);
            return Conflict(new { error = "Payment for this order already exists." });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Payment gateway error for order {OrderId}", request.OrderId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Payment gateway temporarily unavailable. Please try again later." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment for order {OrderId}", request.OrderId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while creating the payment." });
        }
    }

    /// <summary>
    /// Verifies the current status of a payment.
    /// Used to check if payment has been completed by the customer.
    /// </summary>
    /// <param name="id">The payment ID to verify status for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current payment status with details.</returns>
    /// <response code="200">Payment status successfully retrieved.</response>
    /// <response code="400">Invalid payment ID format.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Payment not found.</response>
    /// <response code="503">Payment gateway temporarily unavailable.</response>
    [HttpGet("{id}/status")]
    [ProducesResponseType(typeof(PaymentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetPaymentStatus(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Valid payment ID is required." });

            _logger.LogInformation(
                "Verifying payment status for payment {PaymentId} in tenant {TenantId}",
                id, CurrentTenantId);

            // Verify payment status through service
            var result = await _paymentService.VerifyPaymentAsync(
                id.ToString(),
                string.Empty,  // Signature verification handled in service
                cancellationToken);

            _logger.LogInformation(
                "Payment {PaymentId} status verified: {Status}",
                id, result.IsSuccessful ? "Completed" : "Pending");

            // Return payment status
            var response = new PaymentStatusResponse
            {
                PaymentId = id,
                Status = result.IsSuccessful ? "Completed" : "Pending",
                Amount = result.Amount.Amount,
                Currency = result.Amount.Currency ?? "INR",
                UpdatedAt = result.ProcessedAt
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Payment {PaymentId} not found for tenant {TenantId}", id, CurrentTenantId);
            return NotFound(new { error = "Payment not found." });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Payment gateway error verifying payment {PaymentId}", id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Payment gateway temporarily unavailable. Please try again later." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment {PaymentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while verifying the payment." });
        }
    }

    /// <summary>
    /// Requests a refund for a completed payment.
    /// Can process full or partial refunds.
    /// </summary>
    /// <param name="id">The payment ID to refund.</param>
    /// <param name="request">Refund request with reason and optional amount.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Refund status confirmation.</returns>
    /// <response code="200">Refund successfully processed.</response>
    /// <response code="400">Invalid refund request or cannot refund payment.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Payment not found.</response>
    /// <response code="503">Payment gateway temporarily unavailable.</response>
    [Authorize(Policy = Permissions.BillingWrite)]
    [HttpPost("{id}/refund")]
    [ProducesResponseType(typeof(PaymentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> RequestRefund(
        [FromRoute] Guid id,
        [FromBody] RefundRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Valid payment ID is required." });

            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            if (string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(new { error = "Refund reason is required." });

            _logger.LogInformation(
                "Processing refund for payment {PaymentId} in tenant {TenantId}. Reason: {Reason}",
                id, CurrentTenantId, request.Reason);

            // Prepare refund amount (convert to Money object)
            var refundAmount = new Domain.ValueObjects.Money(
                request.Amount ?? 0,  // 0 indicates full refund
                "INR"
            );

            // Process refund through service
            var result = await _paymentService.RefundAsync(
                id.ToString(),
                refundAmount,
                cancellationToken);

            _logger.LogInformation(
                "Refund processed successfully for payment {PaymentId}. Refund ID: {RefundId}",
                id, result.RefundId);

            // Return refund confirmation
            var response = new PaymentStatusResponse
            {
                PaymentId = id,
                Status = "Refunded",
                Amount = result.Amount.Amount,
                Currency = result.Amount.Currency ?? "INR",
                UpdatedAt = result.ProcessedAt
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Payment {PaymentId} not found for refund in tenant {TenantId}", 
                id, CurrentTenantId);
            return NotFound(new { error = "Payment not found." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot process refund for payment {PaymentId}: {Reason}", id, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Payment gateway error processing refund for payment {PaymentId}", id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Payment gateway temporarily unavailable. Please try again later." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while processing the refund." });
        }
    }
}
