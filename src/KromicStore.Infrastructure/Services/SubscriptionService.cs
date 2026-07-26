namespace KromicStore.Infrastructure.Services;

using Application.Interfaces;
using Contracts.V1.Subscriptions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for subscription management.
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SubscriptionService> _logger;

    /// <summary>
    /// Subscription plan definitions with pricing and features.
    /// </summary>
    private static readonly Dictionary<int, (string Name, SubscriptionPlan Plan, decimal Price)> PlanDefinitions = new()
    {
        { 1, ("Starter", SubscriptionPlan.Starter, 99m) },
        { 2, ("Professional", SubscriptionPlan.Professional, 299m) },
        { 3, ("Enterprise", SubscriptionPlan.Enterprise, 999m) }
    };

    /// <summary>
    /// Initializes a new instance of the SubscriptionService class.
    /// </summary>
    public SubscriptionService(
        IUnitOfWork unitOfWork,
        ILogger<SubscriptionService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<CurrentSubscriptionResponse?> GetCurrentSubscriptionAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subscriptions = await _unitOfWork.Subscriptions
                .FindAsync(s => s.TenantId == tenantId, cancellationToken);

            var subscription = subscriptions.FirstOrDefault();

            if (subscription == null)
            {
                _logger.LogWarning("No subscription found for tenant {TenantId}", tenantId);
                return null;
            }

            return MapToCurrentSubscriptionResponse(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current subscription for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PlansListResponse> GetAvailablePlansAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subscriptions = await _unitOfWork.Subscriptions
                .FindAsync(s => s.TenantId == tenantId, cancellationToken);

            var currentSubscription = subscriptions.FirstOrDefault();

            var plans = new List<SubscriptionPlanResponse>();
            SubscriptionPlanResponse? currentPlan = null;

            foreach (var (planId, planInfo) in PlanDefinitions)
            {
                var features = SubscriptionPlanFeatures.GetFeaturesForPlan(planInfo.Plan);
                var planResponse = new SubscriptionPlanResponse
                {
                    Id = planId,
                    Name = planInfo.Name,
                    Tier = planInfo.Plan.ToString(),
                    MonthlyPrice = planInfo.Price,
                    MaxUsers = features.MaxUsers,
                    MaxProducts = features.MaxProducts,
                    MaxApiCallsPerMonth = features.MaxApiCallsPerMonth,
                    WebhooksEnabled = features.WebhooksEnabled,
                    AnalyticsEnabled = features.AnalyticsEnabled,
                    IsCurrentPlan = currentSubscription?.PlanType == planInfo.Plan,
                    Features = GetFeaturesList(features)
                };

                plans.Add(planResponse);

                if (currentSubscription?.PlanType == planInfo.Plan)
                {
                    currentPlan = planResponse;
                }
            }

            var comparisonTable = BuildComparisonTable(plans);

            return new PlansListResponse
            {
                Plans = plans,
                CurrentPlan = currentPlan,
                ComparisonTable = comparisonTable
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available plans for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult<CurrentSubscriptionResponse>> UpgradePlanAsync(
        Guid tenantId,
        int newPlanId,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subscriptions = await _unitOfWork.Subscriptions
                .FindAsync(s => s.TenantId == tenantId, cancellationToken);

            var subscription = subscriptions.FirstOrDefault();

            if (subscription == null)
            {
                return ServiceResult<CurrentSubscriptionResponse>.FailureResult(
                    "Subscription not found.");
            }

            if (!PlanDefinitions.TryGetValue(newPlanId, out var newPlanInfo))
            {
                return ServiceResult<CurrentSubscriptionResponse>.FailureResult(
                    "Invalid plan ID.");
            }

            // Verify upgrade is to a higher tier
            if (IsLowerTier(subscription.PlanType, newPlanInfo.Plan))
            {
                return ServiceResult<CurrentSubscriptionResponse>.FailureResult(
                    "Cannot upgrade to a lower tier plan. Use downgrade instead.");
            }

            if (subscription.PlanType == newPlanInfo.Plan)
            {
                return ServiceResult<CurrentSubscriptionResponse>.FailureResult(
                    "Cannot upgrade to the same plan.");
            }

            // Check if subscription is in a valid state
            if (subscription.Status == SubscriptionStatus.Cancelled ||
                subscription.Status == SubscriptionStatus.Suspended)
            {
                return ServiceResult<CurrentSubscriptionResponse>.FailureResult(
                    $"Cannot upgrade subscription in {subscription.Status} state.");
            }

            if (subscription.Status == SubscriptionStatus.GracePeriod)
            {
                return ServiceResult<CurrentSubscriptionResponse>.FailureResult(
                    "Cannot upgrade subscription pending cancellation.");
            }

            // Update the subscription plan
            var newMoney = new Money(newPlanInfo.Price);
            subscription.ChangePlan(newPlanInfo.Plan, newMoney);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Upgraded subscription for tenant {TenantId} from {OldPlan} to {NewPlan}",
                tenantId, subscription.PlanType, newPlanInfo.Plan);

            return ServiceResult<CurrentSubscriptionResponse>.SuccessResult(
                MapToCurrentSubscriptionResponse(subscription));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upgrading plan for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult<CurrentSubscriptionResponse>> DowngradePlanAsync(
        Guid tenantId,
        int newPlanId,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subscriptions = await _unitOfWork.Subscriptions
                .FindAsync(s => s.TenantId == tenantId, cancellationToken);

            var subscription = subscriptions.FirstOrDefault();

            if (subscription == null)
            {
                return ServiceResult<CurrentSubscriptionResponse>.FailureResult(
                    "Subscription not found.");
            }

            if (!PlanDefinitions.TryGetValue(newPlanId, out var newPlanInfo))
            {
                return ServiceResult<CurrentSubscriptionResponse>.FailureResult(
                    "Invalid plan ID.");
            }

            // Verify downgrade is to a lower tier
            if (IsHigherOrEqualTier(subscription.PlanType, newPlanInfo.Plan))
            {
                return ServiceResult<CurrentSubscriptionResponse>.FailureResult(
                    "Cannot downgrade to a higher or equal tier plan. Use upgrade instead.");
            }

            // Check if subscription is in a valid state
            if (subscription.Status == SubscriptionStatus.Cancelled ||
                subscription.Status == SubscriptionStatus.Suspended)
            {
                return ServiceResult<CurrentSubscriptionResponse>.FailureResult(
                    $"Cannot downgrade subscription in {subscription.Status} state.");
            }

            if (subscription.Status == SubscriptionStatus.GracePeriod)
            {
                return ServiceResult<CurrentSubscriptionResponse>.FailureResult(
                    "Cannot downgrade subscription pending cancellation.");
            }

            // Update the subscription plan
            var newMoney = new Money(newPlanInfo.Price);
            subscription.ChangePlan(newPlanInfo.Plan, newMoney);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Downgraded subscription for tenant {TenantId} from {OldPlan} to {NewPlan}",
                tenantId, subscription.PlanType, newPlanInfo.Plan);

            return ServiceResult<CurrentSubscriptionResponse>.SuccessResult(
                MapToCurrentSubscriptionResponse(subscription));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downgrading plan for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult<CurrentSubscriptionResponse>> CancelSubscriptionAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subscriptions = await _unitOfWork.Subscriptions
                .FindAsync(s => s.TenantId == tenantId, cancellationToken);

            var subscription = subscriptions.FirstOrDefault();

            if (subscription == null)
            {
                return ServiceResult<CurrentSubscriptionResponse>.FailureResult(
                    "Subscription not found.");
            }

            // Check if already pending cancellation
            if (subscription.Status == SubscriptionStatus.GracePeriod)
            {
                return ServiceResult<CurrentSubscriptionResponse>.FailureResult(
                    "Subscription is already pending cancellation.");
            }

            // Check if already cancelled
            if (subscription.Status == SubscriptionStatus.Cancelled)
            {
                return ServiceResult<CurrentSubscriptionResponse>.FailureResult(
                    "Subscription is already cancelled.");
            }

            // Initiate cancellation with 30-day grace period
            subscription.InitiateCancellation();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Initiated cancellation for tenant {TenantId} subscription. Grace period until {GracePeriodEndsAt}",
                tenantId, subscription.GracePeriodEndsAt);

            return ServiceResult<CurrentSubscriptionResponse>.SuccessResult(
                MapToCurrentSubscriptionResponse(subscription));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult<CurrentSubscriptionResponse>> ReactivateSubscriptionAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subscriptions = await _unitOfWork.Subscriptions
                .FindAsync(s => s.TenantId == tenantId, cancellationToken);

            var subscription = subscriptions.FirstOrDefault();

            if (subscription == null)
            {
                return ServiceResult<CurrentSubscriptionResponse>.FailureResult(
                    "Subscription not found.");
            }

            // Check if subscription is pending cancellation
            if (subscription.Status != SubscriptionStatus.GracePeriod)
            {
                return ServiceResult<CurrentSubscriptionResponse>.FailureResult(
                    "Subscription is not pending cancellation.");
            }

            // Check if grace period has expired
            if (subscription.GracePeriodEndsAt.HasValue && DateTime.UtcNow > subscription.GracePeriodEndsAt)
            {
                return ServiceResult<CurrentSubscriptionResponse>.FailureResult(
                    "Grace period has expired. Cannot reactivate subscription.");
            }

            // Reactivate the subscription
            subscription.Reactivate();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Reactivated subscription for tenant {TenantId}",
                tenantId);

            return ServiceResult<CurrentSubscriptionResponse>.SuccessResult(
                MapToCurrentSubscriptionResponse(subscription));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating subscription for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Maps a Subscription entity to CurrentSubscriptionResponse.
    /// </summary>
    private static CurrentSubscriptionResponse MapToCurrentSubscriptionResponse(Subscription subscription)
    {
        var features = SubscriptionPlanFeatures.GetFeaturesForPlan(subscription.PlanType);

        var billingCycleStart = subscription.StartDate;
        var billingCycleEnd = billingCycleStart.AddMonths(1);
        var nextBillingDate = billingCycleEnd;

        return new CurrentSubscriptionResponse
        {
            Id = subscription.Id,
            PlanName = subscription.PlanType.ToString(),
            Tier = subscription.PlanType.ToString(),
            Status = subscription.Status.ToString(),
            BillingCycleStart = billingCycleStart,
            BillingCycleEnd = billingCycleEnd,
            NextBillingDate = nextBillingDate,
            MonthlyPrice = subscription.MonthlyPrice.Amount,
            Features = GetFeaturesList(features),
            CancellationRequestedDate = subscription.Status == SubscriptionStatus.GracePeriod ? DateTime.UtcNow : null,
            ScheduledDeletionDate = subscription.GracePeriodEndsAt,
            TrialEndsAt = subscription.TrialEndsAt,
            MaxUsers = subscription.MaxUsers,
            MaxProducts = subscription.MaxProducts,
            MaxApiCallsPerMonth = subscription.MaxApiCallsPerMonth,
            WebhooksEnabled = subscription.WebhooksEnabled,
            AnalyticsEnabled = subscription.AnalyticsEnabled
        };
    }

    /// <summary>
    /// Gets a list of feature descriptions for a plan.
    /// </summary>
    private static List<string> GetFeaturesList(SubscriptionPlanFeatures features)
    {
        var featuresList = new List<string>
        {
            $"Up to {features.MaxUsers} users",
            $"Up to {features.MaxProducts} products",
            $"{features.MaxApiCallsPerMonth:N0} API calls/month"
        };

        if (features.WebhooksEnabled)
            featuresList.Add("Webhooks enabled");

        if (features.AnalyticsEnabled)
            featuresList.Add("Advanced analytics");

        return featuresList;
    }

    /// <summary>
    /// Builds a comparison table for all available plans.
    /// </summary>
    private static Dictionary<string, Dictionary<string, object>> BuildComparisonTable(
        List<SubscriptionPlanResponse> plans)
    {
        var comparison = new Dictionary<string, Dictionary<string, object>>();

        comparison["Monthly Price"] = plans.ToDictionary(
            p => p.Name,
            p => (object)$"${p.MonthlyPrice:F2}");

        comparison["Max Users"] = plans.ToDictionary(
            p => p.Name,
            p => (object)p.MaxUsers);

        comparison["Max Products"] = plans.ToDictionary(
            p => p.Name,
            p => (object)p.MaxProducts);

        comparison["API Calls/Month"] = plans.ToDictionary(
            p => p.Name,
            p => (object)$"{p.MaxApiCallsPerMonth:N0}");

        comparison["Webhooks"] = plans.ToDictionary(
            p => p.Name,
            p => (object)(p.WebhooksEnabled ? "Yes" : "No"));

        comparison["Analytics"] = plans.ToDictionary(
            p => p.Name,
            p => (object)(p.AnalyticsEnabled ? "Yes" : "No"));

        return comparison;
    }

    /// <summary>
    /// Determines if the new plan is a lower tier than the current plan.
    /// </summary>
    private static bool IsLowerTier(SubscriptionPlan current, SubscriptionPlan newPlan)
    {
        var tierOrder = new[] { SubscriptionPlan.Starter, SubscriptionPlan.Professional, SubscriptionPlan.Enterprise };
        var currentIndex = System.Array.IndexOf(tierOrder, current);
        var newIndex = System.Array.IndexOf(tierOrder, newPlan);
        return newIndex < currentIndex;
    }

    /// <summary>
    /// Determines if the new plan is higher or equal tier than the current plan.
    /// </summary>
    private static bool IsHigherOrEqualTier(SubscriptionPlan current, SubscriptionPlan newPlan)
    {
        var tierOrder = new[] { SubscriptionPlan.Starter, SubscriptionPlan.Professional, SubscriptionPlan.Enterprise };
        var currentIndex = System.Array.IndexOf(tierOrder, current);
        var newIndex = System.Array.IndexOf(tierOrder, newPlan);
        return newIndex >= currentIndex;
    }
}
