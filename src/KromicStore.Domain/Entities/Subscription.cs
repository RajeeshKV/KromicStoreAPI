namespace KromicStore.Domain.Entities;

using Enums;
using ValueObjects;

/// <summary>
/// Represents a subscription plan for a tenant.
/// </summary>
public class Subscription : BaseEntity
{
    /// <summary>Gets the tenant ID this subscription belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the subscription plan type.</summary>
    public SubscriptionPlan PlanType { get; private set; }

    /// <summary>Gets the monthly price for the subscription.</summary>
    public Money MonthlyPrice { get; private set; }

    /// <summary>Gets the subscription start date.</summary>
    public DateTime StartDate { get; private set; }

    /// <summary>Gets the subscription end date (null for active subscriptions).</summary>
    public DateTime? EndDate { get; private set; }

    /// <summary>Gets the current subscription status.</summary>
    public SubscriptionStatus Status { get; private set; }

    /// <summary>Gets the trial end date (null if not in trial).</summary>
    public DateTime? TrialEndsAt { get; private set; }

    /// <summary>Gets the date when grace period ends (null if no grace period).</summary>
    public DateTime? GracePeriodEndsAt { get; private set; }

    /// <summary>Gets the maximum number of users allowed.</summary>
    public int MaxUsers { get; private set; }

    /// <summary>Gets the maximum number of products allowed.</summary>
    public int MaxProducts { get; private set; }

    /// <summary>Gets the maximum API calls per month allowed.</summary>
    public int MaxApiCallsPerMonth { get; private set; }

    /// <summary>Gets a value indicating whether webhooks are enabled.</summary>
    public bool WebhooksEnabled { get; private set; }

    /// <summary>Gets a value indicating whether analytics features are enabled.</summary>
    public bool AnalyticsEnabled { get; private set; }

    /// <summary>Gets the billing cycle day (1-31, for monthly subscriptions).</summary>
    public int BillingCycleDay { get; private set; }

    /// <summary>Gets the last renewal date.</summary>
    public DateTime? LastRenewalAt { get; private set; }

    /// <summary>Gets the Razorpay subscription ID for recurring billing.</summary>
    public string? RazorpaySubscriptionId { get; private set; }

    /// <summary>Gets the Razorpay customer ID.</summary>
    public string? RazorpayCustomerId { get; private set; }

    /// <summary>Gets the date of the last payment.</summary>
    public DateTime? LastPaymentDate { get; private set; }

    /// <summary>Gets the date of the next scheduled payment.</summary>
    public DateTime? NextPaymentDate { get; private set; }

    /// <summary>Gets the payment status (Active, Failed, Pending).</summary>
    public string PaymentStatus { get; private set; } = "Pending";

    /// <summary>Gets the count of consecutive failed payment attempts.</summary>
    public int FailedPaymentCount { get; private set; } = 0;

    /// <summary>
    /// Creates a new instance of Subscription for a regular (paid) plan.
    /// </summary>
    public static Subscription Create(
        Guid tenantId,
        SubscriptionPlan planType,
        Money monthlyPrice,
        DateTime startDate)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (monthlyPrice.Amount <= 0)
            throw new ArgumentException("Monthly price must be greater than zero.", nameof(monthlyPrice));

        var features = SubscriptionPlanFeatures.GetFeaturesForPlan(planType);

        return new Subscription
        {
            TenantId = tenantId,
            PlanType = planType,
            MonthlyPrice = monthlyPrice,
            StartDate = startDate,
            Status = SubscriptionStatus.Active,
            MaxUsers = features.MaxUsers,
            MaxProducts = features.MaxProducts,
            MaxApiCallsPerMonth = features.MaxApiCallsPerMonth,
            WebhooksEnabled = features.WebhooksEnabled,
            AnalyticsEnabled = features.AnalyticsEnabled,
            BillingCycleDay = startDate.Day
        };
    }

    /// <summary>
    /// Creates a new trial subscription for a tenant.
    /// </summary>
    public static Subscription CreateTrial(
        Guid tenantId,
        int trialDays = 14,
        SubscriptionPlan planType = SubscriptionPlan.Starter)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (trialDays <= 0)
            throw new ArgumentException("Trial days must be positive.", nameof(trialDays));

        var features = SubscriptionPlanFeatures.GetFeaturesForPlan(planType);
        var now = DateTime.UtcNow;

        return new Subscription
        {
            TenantId = tenantId,
            PlanType = planType,
            MonthlyPrice = new Money(0),
            StartDate = now,
            Status = SubscriptionStatus.Trial,
            TrialEndsAt = now.AddDays(trialDays),
            MaxUsers = features.MaxUsers,
            MaxProducts = features.MaxProducts,
            MaxApiCallsPerMonth = features.MaxApiCallsPerMonth,
            WebhooksEnabled = features.WebhooksEnabled,
            AnalyticsEnabled = features.AnalyticsEnabled,
            BillingCycleDay = now.Day
        };
    }

    /// <summary>
    /// Upgrades or downgrades the subscription plan.
    /// </summary>
    public void ChangePlan(SubscriptionPlan newPlanType, Money newMonthlyPrice)
    {
        if (Status == SubscriptionStatus.Cancelled)
            throw new InvalidOperationException("Cannot change plan for cancelled subscriptions.");

        var features = SubscriptionPlanFeatures.GetFeaturesForPlan(newPlanType);

        PlanType = newPlanType;
        MonthlyPrice = newMonthlyPrice;
        MaxUsers = features.MaxUsers;
        MaxProducts = features.MaxProducts;
        MaxApiCallsPerMonth = features.MaxApiCallsPerMonth;
        WebhooksEnabled = features.WebhooksEnabled;
        AnalyticsEnabled = features.AnalyticsEnabled;
    }

    /// <summary>
    /// Marks the subscription as suspended (due to payment failure, etc).
    /// </summary>
    public void Suspend(string reason = "")
    {
        if (Status == SubscriptionStatus.Suspended)
            throw new InvalidOperationException("Subscription is already suspended.");

        Status = SubscriptionStatus.Suspended;
    }

    /// <summary>
    /// Resumes a suspended subscription.
    /// </summary>
    public void Resume()
    {
        if (Status != SubscriptionStatus.Suspended)
            throw new InvalidOperationException("Only suspended subscriptions can be resumed.");

        Status = SubscriptionStatus.Active;
    }

    /// <summary>
    /// Initiates cancellation with grace period (30 days).
    /// </summary>
    public void InitiateCancellation()
    {
        if (Status == SubscriptionStatus.Cancelled || Status == SubscriptionStatus.GracePeriod)
            throw new InvalidOperationException("Subscription is already cancelled or in grace period.");

        Status = SubscriptionStatus.GracePeriod;
        GracePeriodEndsAt = DateTime.UtcNow.AddDays(30);
    }

    /// <summary>
    /// Completes the cancellation after grace period.
    /// </summary>
    public void CompleteCancellation()
    {
        Status = SubscriptionStatus.Cancelled;
        EndDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactivates a cancelled subscription (can only reactivate within grace period).
    /// </summary>
    public void Reactivate()
    {
        if (Status != SubscriptionStatus.GracePeriod)
            throw new InvalidOperationException("Only subscriptions in grace period can be reactivated.");

        if (GracePeriodEndsAt.HasValue && DateTime.UtcNow > GracePeriodEndsAt)
            throw new InvalidOperationException("Grace period has expired. Cannot reactivate.");

        Status = SubscriptionStatus.Active;
        GracePeriodEndsAt = null;
    }

    /// <summary>
    /// Records a subscription renewal.
    /// </summary>
    public void RecordRenewal()
    {
        if (Status != SubscriptionStatus.Active)
            throw new InvalidOperationException("Only active subscriptions can be renewed.");

        LastRenewalAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the subscription is in trial period.
    /// </summary>
    public bool IsInTrial()
    {
        return Status == SubscriptionStatus.Trial && TrialEndsAt.HasValue && DateTime.UtcNow < TrialEndsAt;
    }

    /// <summary>
    /// Checks if the trial period has expired.
    /// </summary>
    public bool HasTrialExpired()
    {
        return Status == SubscriptionStatus.Trial && TrialEndsAt.HasValue && DateTime.UtcNow >= TrialEndsAt;
    }

    /// <summary>
    /// Gets the days remaining in trial period (0 if not in trial or expired).
    /// </summary>
    public int GetTrialDaysRemaining()
    {
        if (!IsInTrial())
            return 0;

        return (int)(TrialEndsAt!.Value - DateTime.UtcNow).TotalDays;
    }

    /// <summary>
    /// Ends the trial and converts to a paid subscription.
    /// </summary>
    public void EndTrial(Money monthlyPrice)
    {
        if (Status != SubscriptionStatus.Trial)
            throw new InvalidOperationException("Only trial subscriptions can end trial.");

        Status = SubscriptionStatus.Active;
        TrialEndsAt = null;
        MonthlyPrice = monthlyPrice;
    }

    /// <summary>
    /// Associates this subscription with a Razorpay subscription.
    /// </summary>
    public void LinkRazorpaySubscription(string razorpaySubscriptionId, string customerId, DateTime nextPaymentDate)
    {
        if (string.IsNullOrWhiteSpace(razorpaySubscriptionId))
            throw new ArgumentException("Razorpay subscription ID is required", nameof(razorpaySubscriptionId));
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Customer ID is required", nameof(customerId));

        RazorpaySubscriptionId = razorpaySubscriptionId;
        RazorpayCustomerId = customerId;
        NextPaymentDate = nextPaymentDate;
        PaymentStatus = "Active";
        FailedPaymentCount = 0;
    }

    /// <summary>
    /// Records a successful payment.
    /// </summary>
    public void RecordPayment(DateTime paymentDate, DateTime nextPaymentDate)
    {
        LastPaymentDate = paymentDate;
        NextPaymentDate = nextPaymentDate;
        PaymentStatus = "Active";
        FailedPaymentCount = 0;
    }

    /// <summary>
    /// Records a failed payment attempt.
    /// </summary>
    public void RecordPaymentFailure()
    {
        FailedPaymentCount++;
        PaymentStatus = "Failed";
    }

    /// <summary>
    /// Resets failed payment count.
    /// </summary>
    public void ResetPaymentFailures()
    {
        FailedPaymentCount = 0;
        PaymentStatus = "Active";
    }
}
