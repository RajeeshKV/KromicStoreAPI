namespace KromicStore.Contracts.V1.Payments;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request to process a refund for a payment.
/// </summary>
public class RefundRequest
{
    /// <summary>
    /// Gets or sets the reason for the refund.
    /// </summary>
    [Required(ErrorMessage = "Refund reason is required")]
    [StringLength(500, ErrorMessage = "Reason must not exceed 500 characters")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the refund amount.
    /// If not specified, the full payment amount will be refunded (partial refund support).
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Refund amount must be greater than zero")]
    public decimal? Amount { get; set; }
}
