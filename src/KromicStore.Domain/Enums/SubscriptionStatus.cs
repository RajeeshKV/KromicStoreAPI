namespace KromicStore.Domain.Enums;

/// <summary>
/// Enumeration of subscription statuses in the system.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>Subscription is in trial period.</summary>
    Trial = 1,

    /// <summary>Subscription is active and paid.</summary>
    Active = 2,

    /// <summary>Subscription is suspended (payment failed, etc).</summary>
    Suspended = 3,

    /// <summary>Subscription has been cancelled by user.</summary>
    Cancelled = 4,

    /// <summary>Subscription is in grace period before cancellation.</summary>
    GracePeriod = 5
}
