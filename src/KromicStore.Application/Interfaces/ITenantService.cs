using KromicStore.Application.DTOs.Tenant;

namespace KromicStore.Application.Interfaces
{
    /// <summary>
    /// Service interface for tenant management operations.
    /// Handles tenant registration, retrieval, updates, and account status management.
    /// </summary>
    public interface ITenantService
    {
        /// <summary>
        /// Registers a new tenant with initial admin user and default configuration.
        /// Creates a Tenant entity, TenantAdmin user, Trial subscription, and default configurations.
        /// </summary>
        /// <param name="request">Tenant registration details including company name, contact info, and admin credentials</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Registration response containing tenant ID, API credentials, and access token</returns>
        Task<TenantRegistrationResponse> RegisterAsync(RegisterTenantRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves tenant details by ID.
        /// </summary>
        /// <param name="tenantId">Unique identifier of the tenant</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Tenant details if found; null if tenant does not exist</returns>
        Task<TenantResponse?> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates tenant information such as company name and country.
        /// Only TenantAdmin can perform this operation.
        /// </summary>
        /// <param name="tenantId">Unique identifier of the tenant to update</param>
        /// <param name="request">Updated tenant information</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Updated tenant details</returns>
        Task<TenantResponse> UpdateTenantAsync(Guid tenantId, UpdateTenantRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Suspends a tenant account, preventing all operations until reactivated.
        /// Only SuperUser can suspend tenants.
        /// </summary>
        /// <param name="tenantId">Unique identifier of the tenant to suspend</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>True if suspension was successful; false if tenant not found or already suspended</returns>
        Task<bool> SuspendTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reactivates a suspended tenant account, restoring full access.
        /// Only SuperUser can reactivate tenants.
        /// </summary>
        /// <param name="tenantId">Unique identifier of the tenant to reactivate</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>True if reactivation was successful; false if tenant not found or not suspended</returns>
        Task<bool> ReactivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    }
}
