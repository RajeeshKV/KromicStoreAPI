using System.ComponentModel.DataAnnotations;

namespace KromicStore.Application.DTOs.Tenant
{
    /// <summary>
    /// Request DTO for updating tenant information.
    /// All fields are optional - only provided fields will be updated.
    /// </summary>
    public class UpdateTenantRequest
    {
        /// <summary>
        /// New company name.
        /// Optional. If provided, must be between 2 and 100 characters.
        /// </summary>
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Company name must be between 2 and 100 characters")]
        public string? CompanyName { get; set; }

        /// <summary>
        /// New country code (ISO 3166-1 alpha-2).
        /// Optional. If provided, must be a valid ISO country code.
        /// Examples: "US", "IN", "UK", "DE"
        /// </summary>
        [StringLength(2, MinimumLength = 2, ErrorMessage = "Country must be a valid ISO 3166-1 alpha-2 code")]
        public string? Country { get; set; }
    }
}
