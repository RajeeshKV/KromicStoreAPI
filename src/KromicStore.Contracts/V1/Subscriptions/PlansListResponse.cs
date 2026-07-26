namespace KromicStore.Contracts.V1.Subscriptions;

using System.Collections.Generic;

/// <summary>
/// Response containing list of all available subscription plans with current plan info.
/// </summary>
public class PlansListResponse
{
    /// <summary>
    /// Gets or sets the list of available plans.
    /// </summary>
    public List<SubscriptionPlanResponse> Plans { get; set; } = new();

    /// <summary>
    /// Gets or sets information about the current plan.
    /// </summary>
    public SubscriptionPlanResponse? CurrentPlan { get; set; }

    /// <summary>
    /// Gets or sets the comparison table showing features across all plans.
    /// </summary>
    public Dictionary<string, Dictionary<string, object>> ComparisonTable { get; set; } = new();
}
