// Copyright (c) KromicStore. All rights reserved.

using KromicStore.Domain.Entities;

namespace KromicStore.Application.Interfaces;

/// <summary>
/// Interface for subscription payment operations (recurring billing).
/// </summary>
public interface ISubscriptionPaymentService
{
    /// <summary>Creates a recurring payment mandate for subscription.</summary>
    Task<ServiceResult<SubscriptionPaymentResponse>> CreateSubscriptionMandateAsync(
        Guid tenantId,
        Guid subscriptionId,
        decimal monthlyAmountInRupees,
        CancellationToken cancellationToken = default);

    /// <summary>Handles successful payment from webhook.</summary>
    Task<ServiceResult<bool>> HandlePaymentSuccessAsync(
        string razorpaySubscriptionId,
        DateTime paymentDate,
        DateTime nextPaymentDate,
        CancellationToken cancellationToken = default);

    /// <summary>Handles failed payment from webhook.</summary>
    Task<ServiceResult<bool>> HandlePaymentFailureAsync(
        string razorpaySubscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>Enters grace period after payment failures.</summary>
    Task<ServiceResult<bool>> EnterGracePeriodAsync(
        Guid tenantId,
        Guid subscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>Exits grace period when payment successful.</summary>
    Task<ServiceResult<bool>> ExitGracePeriodAsync(
        Guid tenantId,
        Guid subscriptionId,
        CancellationToken cancellationToken = default);
}

/// <summary>Response from subscription payment creation.</summary>
public record SubscriptionPaymentResponse(
    string RazorpaySubscriptionId,
    string RazorpayCustomerId,
    string PaymentLink,
    DateTime NextPaymentDate);
