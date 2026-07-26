#nullable disable

namespace KromicStore.Contracts.V1.Orders;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for address information.
/// Used when creating or updating orders with address details.
/// </summary>
public class AddressRequest
{
    /// <summary>
    /// The street address line.
    /// </summary>
    [Required(ErrorMessage = "Street address is required")]
    [StringLength(100, MinimumLength = 1, 
        ErrorMessage = "Street address must be between 1 and 100 characters")]
    public string Street { get; set; }

    /// <summary>
    /// The city name.
    /// </summary>
    [Required(ErrorMessage = "City is required")]
    [StringLength(50, MinimumLength = 1, 
        ErrorMessage = "City must be between 1 and 50 characters")]
    public string City { get; set; }

    /// <summary>
    /// The state or province.
    /// </summary>
    [Required(ErrorMessage = "State is required")]
    [StringLength(50, MinimumLength = 1, 
        ErrorMessage = "State must be between 1 and 50 characters")]
    public string State { get; set; }

    /// <summary>
    /// The postal or zip code.
    /// </summary>
    [Required(ErrorMessage = "Postal code is required")]
    [StringLength(20, MinimumLength = 1, 
        ErrorMessage = "Postal code must be between 1 and 20 characters")]
    public string PostalCode { get; set; }

    /// <summary>
    /// The country name.
    /// </summary>
    [Required(ErrorMessage = "Country is required")]
    [StringLength(50, MinimumLength = 1, 
        ErrorMessage = "Country must be between 1 and 50 characters")]
    public string Country { get; set; }
}
