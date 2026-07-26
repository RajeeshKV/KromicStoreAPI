#nullable disable

namespace KromicStore.Contracts.V1.Orders;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for shipping an order.
/// </summary>
public class ShipOrderRequest
{
    /// <summary>
    /// The tracking number for the shipment.
    /// </summary>
    [Required(ErrorMessage = "Tracking number is required")]
    [StringLength(100, ErrorMessage = "Tracking number must not exceed 100 characters")]
    public string TrackingNumber { get; set; }
}
