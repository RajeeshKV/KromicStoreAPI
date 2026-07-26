namespace KromicStore.Application.Interfaces;

using KromicStore.Contracts.V1.Subscriptions;

/// <summary>
/// Interface for subscription management.
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Gets the current subscription details for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Current subscription response or null if not found.</returns>
    Task<CurrentSubscriptionResponse?> GetCurrentSubscriptionAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available subscription plans with feature comparison for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Plans list response with comparison.</returns>
    Task<PlansListResponse> GetAvailablePlansAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upgrades the subscription to a higher plan.
    /// If a pro-rata charge is required, triggers payment automatically.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="newPlanId">The new plan ID to upgrade to.</param>
    /// <param name="effectiveDate">Optional effective date for the upgrade.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Service result with updated subscription or error.</returns>
    Task<ServiceResult<CurrentSubscriptionResponse>> UpgradePlanAsync(
        Guid tenantId,
        int newPlanId,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downgrades the subscription to a lower plan.
    /// A credit is applied to the next billing cycle for the price difference.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="newPlanId">The new plan ID to downgrade to.</param>
    /// <param name="effectiveDate">Optional effective date for the downgrade.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Service result with updated subscription or error.</returns>
    Task<ServiceResult<CurrentSubscriptionResponse>> DowngradePlanAsync(
        Guid tenantId,
        int newPlanId,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests subscription cancellation with a 30-day grace period.
    /// After the grace period, the subscription will be fully deactivated.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Service result with updated subscription showing cancellation details or error.</returns>
    Task<ServiceResult<CurrentSubscriptionResponse>> CancelSubscriptionAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reactivates a cancelled subscription during the 30-day grace period.
    /// After the grace period expires, the subscription cannot be reactivated.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Service result with reactivated subscription or error.</returns>
    Task<ServiceResult<CurrentSubscriptionResponse>> ReactivateSubscriptionAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
