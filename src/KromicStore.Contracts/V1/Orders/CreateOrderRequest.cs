#nullable disable

namespace KromicStore.Contracts.V1.Orders;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for creating a new order.
/// </summary>
public class CreateOrderRequest
{
    /// <summary>
    /// The unique identifier of the customer placing the order.
    /// </summary>
    [Required(ErrorMessage = "Customer ID is required")]
    public Guid CustomerId { get; set; }

    /// <summary>
    /// The collection of items to include in the order.
    /// </summary>
    [Required(ErrorMessage = "Order items are required")]
    [MinLength(1, ErrorMessage = "Order must contain at least one item")]
    public List<CreateOrderItemRequest> Items { get; set; }

    /// <summary>
    /// The shipping address for the order.
    /// </summary>
    [Required(ErrorMessage = "Shipping address is required")]
    public AddressRequest ShippingAddress { get; set; }

    /// <summary>
    /// The billing address for the order.
    /// </summary>
    [Required(ErrorMessage = "Billing address is required")]
    public AddressRequest BillingAddress { get; set; }
}
