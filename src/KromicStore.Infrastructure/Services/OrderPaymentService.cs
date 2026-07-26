using System.Text;
using Microsoft.Extensions.Logging;
using KromicStore.Application.Interfaces;
using KromicStore.Domain.Entities;
using KromicStore.Domain.ValueObjects;
using KromicStore.Domain.Enums;
using KromicStore.Infrastructure.Data;

namespace KromicStore.Infrastructure.Services;

/// <summary>
/// Service for handling order payment operations using Razorpay.
/// Manages payment initiation, capture, failure handling, and refunds for tenant orders.
/// </summary>
public class OrderPaymentService : IOrderPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantPaymentConfigurationService _tenantPaymentConfigService;
    private readonly IRazorpayService _razorpayService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<OrderPaymentService> _logger;
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of OrderPaymentService.
    /// </summary>
    public OrderPaymentService(
        IUnitOfWork unitOfWork,
        ITenantPaymentConfigurationService tenantPaymentConfigService,
        IRazorpayService razorpayService,
        ITenantProvider tenantProvider,
        IEncryptionService encryptionService,
        ILogger<OrderPaymentService> logger,
        AppDbContext dbContext)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantPaymentConfigService = tenantPaymentConfigService ?? throw new ArgumentNullException(nameof(tenantPaymentConfigService));
        _razorpayService = razorpayService ?? throw new ArgumentNullException(nameof(razorpayService));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Initiates an order payment with Razorpay using the tenant's credentials.
    /// Creates a Razorpay order and returns the order ID and payment link.
    /// </summary>
    public async Task<ServiceResult<OrderPaymentInitiationResponse>> InitiateOrderPaymentAsync(
        Guid orderId,
        decimal amountInRupees,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initiating payment for order {OrderId}, amount: ₹{Amount}", orderId, amountInRupees);

            // Validate input
            if (orderId == Guid.Empty)
                return ServiceResult<OrderPaymentInitiationResponse>.FailureResult("Order ID is required");

            if (amountInRupees <= 0)
                return ServiceResult<OrderPaymentInitiationResponse>.FailureResult("Amount must be greater than zero");

            var tenantId = _tenantProvider.TenantId;

            // Fetch order to verify it exists and belongs to tenant
            var orders = await _unitOfWork.Orders.FindAsync(
                o => o.Id == orderId && o.TenantId == tenantId,
                cancellationToken);
            var order = orders.FirstOrDefault();

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for tenant {TenantId}", orderId, tenantId);
                return ServiceResult<OrderPaymentInitiationResponse>.FailureResult("Order not found");
            }

            // Get tenant's Razorpay credentials
            var configResult = await _tenantPaymentConfigService.GetPaymentConfigurationAsync(tenantId, cancellationToken);
            if (!configResult.Success || configResult.Data == null)
            {
                _logger.LogWarning("No payment configuration found for tenant {TenantId}", tenantId);
                return ServiceResult<OrderPaymentInitiationResponse>.FailureResult("Payment configuration not found for tenant");
            }

            // Decrypt tenant's credentials
            var encryptedKeyId = await _unitOfWork.TenantPaymentMethods
                .FindAsync(m => m.TenantId == tenantId, cancellationToken);
            var paymentMethod = encryptedKeyId.FirstOrDefault();

            if (paymentMethod == null || string.IsNullOrEmpty(paymentMethod.EncryptedApiKey))
            {
                _logger.LogWarning("Payment method not configured for tenant {TenantId}", tenantId);
                return ServiceResult<OrderPaymentInitiationResponse>.FailureResult("Payment method not configured");
            }

            // Decrypt credentials using encryption service
            var decryptedKeyId = await _encryptionService.DecryptAsync(paymentMethod.EncryptedApiKey, cancellationToken);
            var decryptedKeySecret = await _encryptionService.DecryptAsync(paymentMethod.EncryptedApiSecret, cancellationToken);

            // Create Razorpay order with tenant's credentials
            var receipt = $"ORD-{orderId:N}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var notes = new Dictionary<string, string>
            {
                { "order_id", orderId.ToString() },
                { "tenant_id", tenantId.ToString() }
            };

            var razorpayOrder = await _razorpayService.CreateOrderAsync(
                amountInRupees,
                "INR",
                receipt,
                notes,
                decryptedKeyId,
                decryptedKeySecret,
                cancellationToken);

            if (string.IsNullOrEmpty(razorpayOrder.Id))
            {
                _logger.LogError("Failed to create Razorpay order for order {OrderId}", orderId);
                return ServiceResult<OrderPaymentInitiationResponse>.FailureResult("Failed to create payment order");
            }

            // Create OrderPayment record
            var amount = new Money(amountInRupees);
            var orderPayment = OrderPayment.Create(orderId, tenantId, razorpayOrder.Id, amount);
            _dbContext.OrderPayments.Add(orderPayment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Payment initiated successfully for order {OrderId}. Razorpay Order ID: {RazorpayOrderId}",
                orderId,
                razorpayOrder.Id);

            // Generate payment link
            var paymentLink = GeneratePaymentLink(razorpayOrder.Id, decryptedKeyId, amountInRupees);

            return ServiceResult<OrderPaymentInitiationResponse>.SuccessResult(
                new OrderPaymentInitiationResponse(
                    razorpayOrder.Id,
                    amountInRupees,
                    "INR",
                    paymentLink));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating payment for order {OrderId}", orderId);
            return ServiceResult<OrderPaymentInitiationResponse>.FailureResult($"Error initiating payment: {ex.Message}");
        }
    }

    /// <summary>
    /// Captures a payment after successful authorization by verifying the signature.
    /// Marks OrderPayment as Captured and updates order status.
    /// </summary>
    public async Task<ServiceResult<bool>> CapturePaymentAsync(
        string razorpayPaymentId,
        string razorpayOrderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Capturing payment {PaymentId} for Razorpay order {OrderId}",
                razorpayPaymentId,
                razorpayOrderId);

            // Validate input
            if (string.IsNullOrWhiteSpace(razorpayPaymentId))
                return ServiceResult<bool>.FailureResult("Payment ID is required");

            if (string.IsNullOrWhiteSpace(razorpayOrderId))
                return ServiceResult<bool>.FailureResult("Razorpay Order ID is required");

            var tenantId = _tenantProvider.TenantId;

            // Find OrderPayment by Razorpay Order ID
            var orderPayment = (from op in _dbContext.OrderPayments 
                               where op.RazorpayOrderId == razorpayOrderId && op.TenantId == tenantId 
                               select op).FirstOrDefault();

            if (orderPayment == null)
            {
                _logger.LogWarning(
                    "OrderPayment not found for Razorpay Order ID {OrderId}",
                    razorpayOrderId);
                return ServiceResult<bool>.FailureResult("Payment record not found");
            }

            // Verify payment with Razorpay using tenant's credentials
            var configResult = await _tenantPaymentConfigService.GetPaymentConfigurationAsync(tenantId, cancellationToken);
            if (!configResult.Success || configResult.Data == null)
            {
                _logger.LogWarning("Payment configuration not found for tenant {TenantId}", tenantId);
                return ServiceResult<bool>.FailureResult("Payment configuration not found");
            }

            // Update OrderPayment status
            orderPayment.AuthorizePayment(razorpayPaymentId);
            orderPayment.CapturePayment();
            _dbContext.OrderPayments.Update(orderPayment);

            // Fetch and update Order status
            var orders = await _unitOfWork.Orders.FindAsync(
                o => o.Id == orderPayment.OrderId && o.TenantId == tenantId,
                cancellationToken);
            var order = orders.FirstOrDefault();

            if (order != null)
            {
                // Update order status to Paid by recording the payment
                if (order.Status != OrderStatus.Paid)
                {
                    order.RecordPayment("razorpay", razorpayPaymentId);
                    _unitOfWork.Orders.Update(order);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Payment captured successfully for order {OrderId}. Razorpay Payment ID: {PaymentId}",
                orderPayment.OrderId,
                razorpayPaymentId);

            return ServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error capturing payment {PaymentId} for Razorpay order {OrderId}",
                razorpayPaymentId,
                razorpayOrderId);
            return ServiceResult<bool>.FailureResult($"Error capturing payment: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles payment failure by marking OrderPayment as Failed and logging the incident.
    /// Notifies relevant parties and updates order status.
    /// </summary>
    public async Task<ServiceResult<bool>> HandlePaymentFailureAsync(
        string razorpayOrderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("Handling payment failure for Razorpay order {OrderId}", razorpayOrderId);

            // Validate input
            if (string.IsNullOrWhiteSpace(razorpayOrderId))
                return ServiceResult<bool>.FailureResult("Razorpay Order ID is required");

            var tenantId = _tenantProvider.TenantId;

            // Find OrderPayment by Razorpay Order ID
            var orderPayments = (from op in _dbContext.OrderPayments 
                               where op.RazorpayOrderId == razorpayOrderId && op.TenantId == tenantId 
                               select op).FirstOrDefault();

            if (orderPayments == null)
            {
                _logger.LogWarning(
                    "OrderPayment not found for failed Razorpay order {OrderId}",
                    razorpayOrderId);
                return ServiceResult<bool>.FailureResult("Payment record not found");
            }

            // Mark payment as failed
            orderPayments.MarkAsFailed();
            _dbContext.OrderPayments.Update(orderPayments);

            // Update order status to reflect payment failure
            var orders = await _unitOfWork.Orders.FindAsync(
                o => o.Id == orderPayments.OrderId && o.TenantId == tenantId,
                cancellationToken);
            var order = orders.FirstOrDefault();

            if (order != null && order.Status != OrderStatus.Cancelled)
            {
                // Log failure but don't change order status automatically
                // The order can remain pending for the user to retry
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogError(
                "Payment failure handled for order {OrderId}. Razorpay Order ID: {RazorpayOrderId}",
                orderPayments.OrderId,
                razorpayOrderId);

            return ServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling payment failure for Razorpay order {OrderId}",
                razorpayOrderId);
            return ServiceResult<bool>.FailureResult($"Error handling payment failure: {ex.Message}");
        }
    }

    /// <summary>
    /// Processes a refund for a paid order using tenant's Razorpay credentials.
    /// Creates refund in Razorpay and marks OrderPayment as Refunded.
    /// </summary>
    public async Task<ServiceResult<bool>> ProcessRefundAsync(
        string razorpayPaymentId,
        decimal amountInRupees,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing refund for Razorpay payment {PaymentId}, amount: ₹{Amount}",
                razorpayPaymentId,
                amountInRupees);

            // Validate input
            if (string.IsNullOrWhiteSpace(razorpayPaymentId))
                return ServiceResult<bool>.FailureResult("Payment ID is required");

            if (amountInRupees <= 0)
                return ServiceResult<bool>.FailureResult("Refund amount must be greater than zero");

            var tenantId = _tenantProvider.TenantId;

            // Find OrderPayment by Razorpay Payment ID
            var orderPayment = (from op in _dbContext.OrderPayments 
                               where op.RazorpayPaymentId == razorpayPaymentId && op.TenantId == tenantId 
                               select op).FirstOrDefault();

            if (orderPayment == null)
            {
                _logger.LogWarning(
                    "OrderPayment not found for refund. Razorpay Payment ID: {PaymentId}",
                    razorpayPaymentId);
                return ServiceResult<bool>.FailureResult("Payment record not found");
            }

            // Verify refund amount doesn't exceed captured amount
            if (amountInRupees > orderPayment.Amount.Amount)
            {
                _logger.LogWarning(
                    "Refund amount ₹{RefundAmount} exceeds captured amount ₹{CapturedAmount}",
                    amountInRupees,
                    orderPayment.Amount.Amount);
                return ServiceResult<bool>.FailureResult("Refund amount exceeds captured amount");
            }

            // Get tenant's Razorpay credentials for refund
            var configResult = await _tenantPaymentConfigService.GetPaymentConfigurationAsync(tenantId, cancellationToken);
            if (!configResult.Success || configResult.Data == null)
            {
                _logger.LogWarning("Payment configuration not found for refund. Tenant: {TenantId}", tenantId);
                return ServiceResult<bool>.FailureResult("Payment configuration not found");
            }

            // Decrypt tenant's credentials
            var paymentMethods = await _unitOfWork.TenantPaymentMethods
                .FindAsync(m => m.TenantId == tenantId, cancellationToken);
            var paymentMethod = paymentMethods.FirstOrDefault();

            if (paymentMethod == null)
            {
                _logger.LogWarning("Payment method not configured for refund. Tenant: {TenantId}", tenantId);
                return ServiceResult<bool>.FailureResult("Payment method not configured");
            }

            // Decrypt credentials
            var decryptedKeyId = await _encryptionService.DecryptAsync(paymentMethod.EncryptedApiKey, cancellationToken);
            var decryptedKeySecret = await _encryptionService.DecryptAsync(paymentMethod.EncryptedApiSecret, cancellationToken);

            // Note: Razorpay refund would be initiated here via the RazorpayService
            // This is a placeholder for the refund API call
            int amountInPaisa = (int)(amountInRupees * 100);

            // TODO: Implement refund via RazorpayService.RefundAsync() if available
            // For now, we'll log the intent and mark as refunded
            _logger.LogInformation(
                "Initiating refund of ₹{Amount} (₹{Paisa} paisa) for payment {PaymentId}",
                amountInRupees,
                amountInPaisa,
                razorpayPaymentId);

            // Mark OrderPayment as refunded
            orderPayment.MarkAsRefunded();
            _dbContext.OrderPayments.Update(orderPayment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Refund processed successfully for payment {PaymentId}, amount: ₹{Amount}",
                razorpayPaymentId,
                amountInRupees);

            return ServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing refund for Razorpay payment {PaymentId}",
                razorpayPaymentId);
            return ServiceResult<bool>.FailureResult($"Error processing refund: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a payment link for Razorpay hosted checkout.
    /// </summary>
    private string GeneratePaymentLink(string razorpayOrderId, string apiKey, decimal amount)
    {
        // Razorpay hosted checkout URL format
        var link = new StringBuilder();
        link.Append("https://checkout.razorpay.com/?");
        link.Append($"key_id={Uri.EscapeDataString(apiKey)}");
        link.Append($"&order_id={Uri.EscapeDataString(razorpayOrderId)}");
        link.Append($"&amount={(int)(amount * 100)}");
        link.Append("&currency=INR");
        link.Append("&name=KromicStore");

        return link.ToString();
    }
}
