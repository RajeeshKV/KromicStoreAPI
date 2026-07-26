// Copyright (c) KromicStore. All rights reserved.

using Microsoft.Extensions.Logging;
using KromicStore.Application.Interfaces;
using KromicStore.Domain.Entities;
using KromicStore.Domain.Enums;

namespace KromicStore.Infrastructure.Services;

/// <summary>
/// Service for subscription payment operations (recurring billing).
/// </summary>
public class SubscriptionPaymentService : ISubscriptionPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRazorpayService _razorpayService;
    private readonly ILogger<SubscriptionPaymentService> _logger;
    private const int GracePeriodDays = 7;
    private const int MaxFailedPaymentAttempts = 3;

    /// <summary>
    /// Initializes a new instance of SubscriptionPaymentService.
    /// </summary>
    public SubscriptionPaymentService(
        IUnitOfWork unitOfWork,
        IRazorpayService razorpayService,
        ILogger<SubscriptionPaymentService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _razorpayService = razorpayService ?? throw new ArgumentNullException(nameof(razorpayService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<SubscriptionPaymentResponse>> CreateSubscriptionMandateAsync(
        Guid tenantId,
        Guid subscriptionId,
        decimal monthlyAmountInRupees,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get subscription and tenant
            var subscriptions = await _unitOfWork.Subscriptions
                .FindAsync(s => s.Id == subscriptionId && s.TenantId == tenantId, cancellationToken);
            var subscription = subscriptions.FirstOrDefault();

            if (subscription == null)
                return ServiceResult<SubscriptionPaymentResponse>.FailureResult("Subscription not found");

            var tenants = await _unitOfWork.Tenants.FindAsync(t => t.Id == tenantId, cancellationToken);
            var tenant = tenants.FirstOrDefault();

            if (tenant == null)
                return ServiceResult<SubscriptionPaymentResponse>.FailureResult("Tenant not found");

            // Create customer in Razorpay
            var customerId = $"cust_{tenantId:N}";
            var notes = new Dictionary<string, string>
            {
                { "tenant_id", tenantId.ToString() },
                { "subscription_id", subscriptionId.ToString() },
                { "plan", subscription.PlanType.ToString() }
            };

            // Create subscription mandate
            var amountInPaisa = (int)(monthlyAmountInRupees * 100);
            var response = await _razorpayService.CreateSubscriptionAsync(
                customerId,
                amountInPaisa,
                $"plan_{subscription.PlanType.ToString().ToLower()}",
                tenant.ContactEmail,
                notes,
                cancellationToken);

            // Link Razorpay subscription to our subscription
            var nextPaymentDate = UnixTimeStampToDateTime(response.CurrentEnd);
            subscription.LinkRazorpaySubscription(response.Id, response.CustomerId, nextPaymentDate);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created Razorpay mandate for subscription {SubscriptionId}: {RazorpaySubscriptionId}",
                subscriptionId, response.Id);

            return ServiceResult<SubscriptionPaymentResponse>.SuccessResult(
                new SubscriptionPaymentResponse(
                    response.Id,
                    response.CustomerId,
                    response.ShortUrl,
                    nextPaymentDate));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription mandate for {SubscriptionId}", subscriptionId);
            return ServiceResult<SubscriptionPaymentResponse>.FailureResult($"Error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> HandlePaymentSuccessAsync(
        string razorpaySubscriptionId,
        DateTime paymentDate,
        DateTime nextPaymentDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Find subscription
            var subscriptions = await _unitOfWork.Subscriptions
                .FindAsync(s => s.RazorpaySubscriptionId == razorpaySubscriptionId, cancellationToken);
            var subscription = subscriptions.FirstOrDefault();

            if (subscription == null)
                return ServiceResult<bool>.FailureResult("Subscription not found");

            // Record payment
            subscription.RecordPayment(paymentDate, nextPaymentDate);

            // Exit grace period if in it
            if (subscription.Status == SubscriptionStatus.GracePeriod)
            {
                subscription.Reactivate();
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Payment recorded for subscription {SubscriptionId}, next payment: {NextPaymentDate}",
                subscription.Id, nextPaymentDate);

            return ServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment success for {RazorpaySubscriptionId}", razorpaySubscriptionId);
            return ServiceResult<bool>.FailureResult($"Error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> HandlePaymentFailureAsync(
        string razorpaySubscriptionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Find subscription
            var subscriptions = await _unitOfWork.Subscriptions
                .FindAsync(s => s.RazorpaySubscriptionId == razorpaySubscriptionId, cancellationToken);
            var subscription = subscriptions.FirstOrDefault();

            if (subscription == null)
                return ServiceResult<bool>.FailureResult("Subscription not found");

            // Record failure
            subscription.RecordPaymentFailure();

            // Check if we should enter grace period
            if (subscription.FailedPaymentCount >= MaxFailedPaymentAttempts)
            {
                _logger.LogWarning(
                    "Payment failed {Count} times for subscription {SubscriptionId}, entering grace period",
                    subscription.FailedPaymentCount, subscription.Id);

                subscription.InitiateCancellation();
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment failure for {RazorpaySubscriptionId}", razorpaySubscriptionId);
            return ServiceResult<bool>.FailureResult($"Error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> EnterGracePeriodAsync(
        Guid tenantId,
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subscriptions = await _unitOfWork.Subscriptions
                .FindAsync(s => s.Id == subscriptionId && s.TenantId == tenantId, cancellationToken);
            var subscription = subscriptions.FirstOrDefault();

            if (subscription == null)
                return ServiceResult<bool>.FailureResult("Subscription not found");

            subscription.InitiateCancellation();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Subscription {SubscriptionId} entered grace period", subscriptionId);
            return ServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error entering grace period for {SubscriptionId}", subscriptionId);
            return ServiceResult<bool>.FailureResult($"Error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> ExitGracePeriodAsync(
        Guid tenantId,
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subscriptions = await _unitOfWork.Subscriptions
                .FindAsync(s => s.Id == subscriptionId && s.TenantId == tenantId, cancellationToken);
            var subscription = subscriptions.FirstOrDefault();

            if (subscription == null)
                return ServiceResult<bool>.FailureResult("Subscription not found");

            if (subscription.Status != SubscriptionStatus.GracePeriod)
                return ServiceResult<bool>.FailureResult("Subscription not in grace period");

            subscription.Reactivate();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Subscription {SubscriptionId} exited grace period", subscriptionId);
            return ServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exiting grace period for {SubscriptionId}", subscriptionId);
            return ServiceResult<bool>.FailureResult($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Converts Unix timestamp to DateTime.
    /// </summary>
    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
        return dateTime;
    }
}
