#nullable disable

namespace KromicStore.Contracts.V1.Customers;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for creating a new customer.
/// </summary>
public class CreateCustomerRequest
{
    /// <summary>
    /// The customer's first name.
    /// </summary>
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, MinimumLength = 1, 
        ErrorMessage = "First name must be between 1 and 50 characters")]
    public string FirstName { get; set; }

    /// <summary>
    /// The customer's last name.
    /// </summary>
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, MinimumLength = 1, 
        ErrorMessage = "Last name must be between 1 and 50 characters")]
    public string LastName { get; set; }

    /// <summary>
    /// The customer's email address (must be unique within tenant).
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email is not in a valid format")]
    public string Email { get; set; }

    /// <summary>
    /// The customer's phone number (optional).
    /// </summary>
    [Phone(ErrorMessage = "Phone number is not in a valid format")]
    public string PhoneNumber { get; set; }
}
