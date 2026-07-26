namespace KromicStore.Contracts.V1.Subscriptions;

using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request to upgrade subscription to a higher plan.
/// </summary>
public class UpgradePlanRequest
{
    /// <summary>
    /// Gets or sets the new plan ID to upgrade to.
    /// </summary>
    [Required(ErrorMessage = "New plan ID is required.")]
    public int NewPlanId { get; set; }

    /// <summary>
    /// Gets or sets the optional effective date for the upgrade.
    /// If not specified, upgrade takes effect immediately.
    /// </summary>
    public DateTime? EffectiveDate { get; set; }
}
