namespace KromicStore.Contracts.V1.Subscriptions;

using System;
using System.Collections.Generic;

/// <summary>
/// Response containing subscription plan details.
/// </summary>
public class SubscriptionPlanResponse
{
    /// <summary>
    /// Gets or sets the plan ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the plan name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plan tier (Starter, Professional, Enterprise).
    /// </summary>
    public string Tier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the monthly price in base currency.
    /// </summary>
    public decimal MonthlyPrice { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of users allowed on this plan.
    /// </summary>
    public int MaxUsers { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of products allowed on this plan.
    /// </summary>
    public int MaxProducts { get; set; }

    /// <summary>
    /// Gets or sets the maximum API calls per month allowed on this plan.
    /// </summary>
    public int MaxApiCallsPerMonth { get; set; }

    /// <summary>
    /// Gets or sets the list of features included in this plan.
    /// </summary>
    public List<string> Features { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether this is the tenant's current plan.
    /// </summary>
    public bool IsCurrentPlan { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether webhooks are enabled on this plan.
    /// </summary>
    public bool WebhooksEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether analytics are enabled on this plan.
    /// </summary>
    public bool AnalyticsEnabled { get; set; }
}
