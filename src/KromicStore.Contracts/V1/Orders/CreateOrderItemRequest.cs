namespace KromicStore.Contracts.V1.Orders;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for an order item to be created.
/// Specifies product and quantity for an order line item.
/// </summary>
public class CreateOrderItemRequest
{
    /// <summary>
    /// The unique identifier of the product to order.
    /// </summary>
    [Required(ErrorMessage = "Product ID is required")]
    public Guid ProductId { get; set; }

    /// <summary>
    /// The quantity of the product to order.
    /// </summary>
    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, int.MaxValue, 
        ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }
}
