#nullable disable

namespace KromicStore.Contracts.V1.Orders;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for updating an existing order.
/// </summary>
public class UpdateOrderRequest
{
    /// <summary>
    /// The updated shipping address for the order.
    /// </summary>
    [Required(ErrorMessage = "Shipping address is required")]
    public AddressRequest ShippingAddress { get; set; }

    /// <summary>
    /// The updated billing address for the order.
    /// </summary>
    [Required(ErrorMessage = "Billing address is required")]
    public AddressRequest BillingAddress { get; set; }

    /// <summary>
    /// The updated collection of items (only if order is still pending).
    /// </summary>
    public List<CreateOrderItemRequest> Items { get; set; }
}
