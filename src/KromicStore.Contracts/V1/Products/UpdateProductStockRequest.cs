namespace KromicStore.Contracts.V1.Products;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for updating product stock quantity.
/// </summary>
public class UpdateProductStockRequest
{
    /// <summary>
    /// The new quantity in stock.
    /// </summary>
    [Required(ErrorMessage = "Quantity is required")]
    [Range(0, int.MaxValue, 
        ErrorMessage = "Quantity cannot be negative")]
    public int Quantity { get; set; }
}
