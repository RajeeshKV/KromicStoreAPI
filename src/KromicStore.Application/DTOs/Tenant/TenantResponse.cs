namespace KromicStore.Application.DTOs.Tenant
{
    /// <summary>
    /// Response DTO containing public tenant information.
    /// Sensitive data like API secrets are not included in this response.
    /// </summary>
    public class TenantResponse
    {
        /// <summary>
        /// Unique identifier for the tenant.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the company/organization.
        /// </summary>
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// Primary contact email address for the tenant.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Country code (ISO 3166-1 alpha-2) where the organization is located.
        /// Examples: "US", "IN", "UK", "DE"
        /// </summary>
        public string Country { get; set; } = string.Empty;

        /// <summary>
        /// Current status of the tenant account.
        /// Possible values: "Active", "Suspended", "Deactivated"
        /// </summary>
        public string Status { get; set; } = "Active";

        /// <summary>
        /// Timestamp when the tenant was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when the tenant was last updated (UTC).
        /// Null if never updated after creation.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
