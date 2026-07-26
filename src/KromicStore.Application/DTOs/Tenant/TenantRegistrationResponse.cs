namespace KromicStore.Application.DTOs.Tenant
{
    /// <summary>
    /// Response DTO containing tenant registration result with API credentials.
    /// The API secret is only returned once at registration and should be stored securely.
    /// </summary>
    public class TenantRegistrationResponse
    {
        /// <summary>
        /// Unique identifier for the newly created tenant.
        /// </summary>
        public Guid TenantId { get; set; }

        /// <summary>
        /// Name of the company/organization.
        /// </summary>
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// Primary contact email address for the tenant.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// JWT access token for immediate API access.
        /// Valid for 24 hours from registration.
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Public API key for service identification.
        /// Used in API requests alongside the private key.
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Private API secret for authentication.
        /// Only returned once at registration. Store securely.
        /// Never expose this in logs or responses after initial registration.
        /// </summary>
        public string? ApiSecret { get; set; }

        /// <summary>
        /// Timestamp when the tenant was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
