using System.ComponentModel.DataAnnotations;

namespace KromicStore.Application.DTOs.Tenant
{
    /// <summary>
    /// Request DTO for tenant registration.
    /// Contains company information and initial admin user credentials.
    /// </summary>
    public class RegisterTenantRequest
    {
        /// <summary>
        /// Name of the company/organization.
        /// Required and must be between 2 and 100 characters.
        /// </summary>
        [Required(ErrorMessage = "Company name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Company name must be between 2 and 100 characters")]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// Subdomain for the tenant (e.g., "mystore" for mystore.kromic.in).
        /// Required and must be unique. Only alphanumeric characters and hyphens allowed.
        /// </summary>
        [Required(ErrorMessage = "Subdomain is required")]
        [StringLength(63, MinimumLength = 3, ErrorMessage = "Subdomain must be between 3 and 63 characters")]
        [RegularExpression(@"^[a-z0-9]([a-z0-9-]*[a-z0-9])?$", ErrorMessage = "Subdomain must contain only alphanumeric characters and hyphens, and must start and end with alphanumeric")]
        public string Subdomain { get; set; } = string.Empty;

        /// <summary>
        /// Primary contact email address for the tenant.
        /// Must be a valid email format and unique across all tenants.
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// First name of the initial admin user.
        /// Required and must be between 1 and 50 characters.
        /// </summary>
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Last name of the initial admin user.
        /// Required and must be between 1 and 50 characters.
        /// </summary>
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 50 characters")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Password for the initial admin user.
        /// Must meet minimum security requirements: at least 8 characters,
        /// including uppercase, lowercase, number, and special character.
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Password confirmation field.
        /// Must match the Password field exactly.
        /// </summary>
        [Required(ErrorMessage = "Password confirmation is required")]
        [Compare("Password", ErrorMessage = "Password and confirmation must match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        /// <summary>
        /// Country code (ISO 3166-1 alpha-2) where the organization is located.
        /// Examples: "US", "IN", "UK", "DE"
        /// Required and must be a valid ISO country code.
        /// </summary>
        [Required(ErrorMessage = "Country is required")]
        [StringLength(2, MinimumLength = 2, ErrorMessage = "Country must be a valid ISO 3166-1 alpha-2 code")]
        public string Country { get; set; } = string.Empty;
    }
}
