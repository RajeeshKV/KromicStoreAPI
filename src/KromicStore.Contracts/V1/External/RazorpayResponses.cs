// Copyright (c) KromicStore. All rights reserved.

namespace KromicStore.Contracts.V1.External;

/// <summary>
/// Response from Razorpay subscription creation/retrieval.
/// </summary>
public record RazorpaySubscriptionResponse(
    string Id,
    string CustomerId,
    int PlannedAmount,
    string Status,
    long StartAt,
    long EndAt,
    long CurrentStart,
    long CurrentEnd,
    int? PausedAt,
    long? ExpireBy,
    string ShortUrl,
    Dictionary<string, object> Notes);

/// <summary>
/// Response from Razorpay order creation.
/// </summary>
public record RazorpayOrderResponse(
    string Id,
    int Amount,
    string AmountPaid,
    string AmountDue,
    string Currency,
    string Receipt,
    string Status,
    Dictionary<string, object> Notes);

/// <summary>
/// Response from Razorpay payment operations.
/// </summary>
public record RazorpayPaymentResponse(
    string Id,
    int Amount,
    string Currency,
    string Status,
    string Method,
    string OrderId,
    string Description,
    Dictionary<string, object> Notes);

/// <summary>
/// Error response from Razorpay API.
/// </summary>
public record RazorpayErrorResponse(
    string Error,
    string Description);

/// <summary>
/// Webhook event received from Razorpay.
/// </summary>
public record RazorpayWebhookEvent(
    string Event,
    Dictionary<string, object> Payload);
