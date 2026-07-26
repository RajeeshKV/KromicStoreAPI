namespace KromicStore.Domain.Enums;

/// <summary>
/// Enumeration of supported webhook event types for external system integration.
/// </summary>
public enum WebhookEventType
{
    /// <summary>
    /// Event triggered when a new order is created.
    /// </summary>
    OrderCreated = 1,

    /// <summary>
    /// Event triggered when an order status changes.
    /// </summary>
    OrderStatusChanged = 2,

    /// <summary>
    /// Event triggered when an order is cancelled.
    /// </summary>
    OrderCancelled = 3,

    /// <summary>
    /// Event triggered when a payment is successfully processed.
    /// </summary>
    PaymentProcessed = 4,

    /// <summary>
    /// Event triggered when a payment fails.
    /// </summary>
    PaymentFailed = 5,

    /// <summary>
    /// Event triggered when a new tenant is created.
    /// </summary>
    TenantCreated = 6,

    /// <summary>
    /// Event triggered when a subscription plan is changed.
    /// </summary>
    SubscriptionChanged = 7,

    /// <summary>
    /// Event triggered when a subscription is cancelled.
    /// </summary>
    SubscriptionCancelled = 8,

    /// <summary>
    /// Event triggered when a product is published.
    /// </summary>
    ProductPublished = 9,

    /// <summary>
    /// Event triggered when a product is unpublished.
    /// </summary>
    ProductUnpublished = 10,

    /// <summary>
    /// Event triggered when a new customer is created.
    /// </summary>
    CustomerCreated = 11
}
