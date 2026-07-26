namespace KromicStore.Contracts.V1.Subscriptions;

using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request to downgrade subscription to a lower plan.
/// A credit will be applied to the next billing cycle for the price difference.
/// </summary>
public class DowngradePlanRequest
{
    /// <summary>
    /// Gets or sets the new plan ID to downgrade to.
    /// </summary>
    [Required(ErrorMessage = "New plan ID is required.")]
    public int NewPlanId { get; set; }

    /// <summary>
    /// Gets or sets the optional effective date for the downgrade.
    /// If not specified, downgrade takes effect immediately with credit applied.
    /// </summary>
    public DateTime? EffectiveDate { get; set; }
}
