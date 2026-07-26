namespace KromicStore.Contracts.V1.Subscriptions;

using System;
using System.Collections.Generic;

/// <summary>
/// Response containing current subscription details for a tenant.
/// </summary>
public class CurrentSubscriptionResponse
{
    /// <summary>
    /// Gets or sets the subscription ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the subscription plan type.
    /// </summary>
    public string PlanName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subscription tier (Starter, Professional, Enterprise).
    /// </summary>
    public string Tier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subscription status (Trial, Active, Suspended, Cancelled, GracePeriod).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the billing cycle start date.
    /// </summary>
    public DateTime BillingCycleStart { get; set; }

    /// <summary>
    /// Gets or sets the billing cycle end date.
    /// </summary>
    public DateTime BillingCycleEnd { get; set; }

    /// <summary>
    /// Gets or sets the next billing date.
    /// </summary>
    public DateTime NextBillingDate { get; set; }

    /// <summary>
    /// Gets or sets the monthly price in base currency.
    /// </summary>
    public decimal MonthlyPrice { get; set; }

    /// <summary>
    /// Gets or sets the list of enabled features for this plan.
    /// </summary>
    public List<string> Features { get; set; } = new();

    /// <summary>
    /// Gets or sets the cancellation requested date (if applicable).
    /// </summary>
    public DateTime? CancellationRequestedDate { get; set; }

    /// <summary>
    /// Gets or sets the scheduled deletion date (30 days after cancellation request).
    /// </summary>
    public DateTime? ScheduledDeletionDate { get; set; }

    /// <summary>
    /// Gets or sets the trial end date (if in trial).
    /// </summary>
    public DateTime? TrialEndsAt { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of users allowed.
    /// </summary>
    public int MaxUsers { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of products allowed.
    /// </summary>
    public int MaxProducts { get; set; }

    /// <summary>
    /// Gets or sets the maximum API calls per month allowed.
    /// </summary>
    public int MaxApiCallsPerMonth { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether webhooks are enabled.
    /// </summary>
    public bool WebhooksEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether analytics are enabled.
    /// </summary>
    public bool AnalyticsEnabled { get; set; }
}
