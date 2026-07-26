// Copyright (c) KromicStore. All rights reserved.

using KromicStore.Contracts.V1.External;

namespace KromicStore.Application.Interfaces;

/// <summary>
/// Interface for Razorpay payment service.
/// </summary>
public interface IRazorpayService
{
    // Subscription operations
    /// <summary>
    /// Creates a subscription in Razorpay for recurring billing.
    /// </summary>
    Task<RazorpaySubscriptionResponse> CreateSubscriptionAsync(
        string customerId,
        int amountInPaisa,
        string planId,
        string customerEmail,
        Dictionary<string, string> notes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a subscription from Razorpay.
    /// </summary>
    Task<RazorpaySubscriptionResponse> GetSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates subscription amount (for upgrades/downgrades).
    /// </summary>
    Task<RazorpaySubscriptionResponse> UpdateSubscriptionAsync(
        string subscriptionId,
        int newAmountInPaisa,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a subscription.
    /// </summary>
    Task<bool> CancelSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses a subscription.
    /// </summary>
    Task<bool> PauseSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a paused subscription.
    /// </summary>
    Task<bool> ResumeSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default);

    // One-time payment operations
    /// <summary>
    /// Creates an order for one-time payment (used by tenants for product sales).
    /// Uses provided API key/secret instead of default credentials.
    /// </summary>
    Task<RazorpayOrderResponse> CreateOrderAsync(
        decimal amountInRupees,
        string currency,
        string receipt,
        Dictionary<string, string> notes,
        string apiKey,
        string apiSecret,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures a payment (finalizes authorized payment).
    /// Uses provided API key/secret for tenant's Razorpay account.
    /// </summary>
    Task<RazorpayPaymentResponse> CapturePaymentAsync(
        string paymentId,
        int amountInPaisa,
        string apiKey,
        string apiSecret,
        CancellationToken cancellationToken = default);

    // Signature verification
    /// <summary>
    /// Verifies a payment signature (for webhooks).
    /// </summary>
    bool VerifySignature(string orderId, string paymentId, string signature, string webhookSecret);
    
    /// <summary>
    /// Verifies webhook signature from Razorpay.
    /// </summary>
    bool VerifyWebhookSignature(string body, string signature, string webhookSecret);
}
