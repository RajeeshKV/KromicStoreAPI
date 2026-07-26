namespace KromicStore.Domain.ValueObjects;

using Enums;

/// <summary>
/// Represents features available for a subscription plan.
/// </summary>
public record SubscriptionPlanFeatures
{
    /// <summary>Gets the maximum number of users allowed in this plan.</summary>
    public int MaxUsers { get; init; }

    /// <summary>Gets the maximum number of products allowed in this plan.</summary>
    public int MaxProducts { get; init; }

    /// <summary>Gets the maximum number of API calls per month allowed in this plan.</summary>
    public int MaxApiCallsPerMonth { get; init; }

    /// <summary>Gets a value indicating whether webhooks are enabled in this plan.</summary>
    public bool WebhooksEnabled { get; init; }

    /// <summary>Gets a value indicating whether analytics features are enabled in this plan.</summary>
    public bool AnalyticsEnabled { get; init; }

    /// <summary>
    /// Creates a new instance of SubscriptionPlanFeatures.
    /// </summary>
    public SubscriptionPlanFeatures(
        int maxUsers,
        int maxProducts,
        int maxApiCallsPerMonth,
        bool webhooksEnabled = true,
        bool analyticsEnabled = false)
    {
        if (maxUsers <= 0)
            throw new ArgumentException("MaxUsers must be greater than zero.", nameof(maxUsers));
        if (maxProducts <= 0)
            throw new ArgumentException("MaxProducts must be greater than zero.", nameof(maxProducts));
        if (maxApiCallsPerMonth <= 0)
            throw new ArgumentException("MaxApiCallsPerMonth must be greater than zero.", nameof(maxApiCallsPerMonth));

        MaxUsers = maxUsers;
        MaxProducts = maxProducts;
        MaxApiCallsPerMonth = maxApiCallsPerMonth;
        WebhooksEnabled = webhooksEnabled;
        AnalyticsEnabled = analyticsEnabled;
    }

    /// <summary>
    /// Gets the default features for the specified subscription plan.
    /// </summary>
    public static SubscriptionPlanFeatures GetFeaturesForPlan(SubscriptionPlan plan)
    {
        return plan switch
        {
            SubscriptionPlan.Starter => new SubscriptionPlanFeatures(
                maxUsers: 5,
                maxProducts: 100,
                maxApiCallsPerMonth: 10000,
                webhooksEnabled: true,
                analyticsEnabled: false),

            SubscriptionPlan.Professional => new SubscriptionPlanFeatures(
                maxUsers: 25,
                maxProducts: 1000,
                maxApiCallsPerMonth: 100000,
                webhooksEnabled: true,
                analyticsEnabled: true),

            SubscriptionPlan.Enterprise => new SubscriptionPlanFeatures(
                maxUsers: 500,
                maxProducts: 50000,
                maxApiCallsPerMonth: 10000000,
                webhooksEnabled: true,
                analyticsEnabled: true),

            _ => throw new ArgumentException($"Unknown plan: {plan}", nameof(plan))
        };
    }
}
