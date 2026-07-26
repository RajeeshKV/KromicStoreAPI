namespace KromicStore.Application.Interfaces;

using KromicStore.Domain.Entities;

/// <summary>
/// Repository interface for managing storefronts.
/// </summary>
public interface IStorefrontRepository
{
    /// <summary>
    /// Retrieves a storefront by its ID.
    /// </summary>
    /// <param name="storefrontId">The storefront ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The storefront entity if found; otherwise null.</returns>
    Task<Storefront?> GetByIdAsync(Guid storefrontId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a storefront by its ID, ensuring it belongs to the specified tenant.
    /// </summary>
    /// <param name="storefrontId">The storefront ID.</param>
    /// <param name="tenantId">The tenant ID (for isolation verification).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The storefront entity if found and belongs to the tenant; otherwise null.</returns>
    Task<Storefront?> GetByIdAsync(Guid storefrontId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all storefronts for a specific tenant with related entities loaded.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of storefronts for the tenant with related pages, sections, and components.</returns>
    Task<List<Storefront>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new storefront to the repository.
    /// </summary>
    /// <param name="storefront">The storefront entity to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AddAsync(Storefront storefront, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing storefront in the repository.
    /// </summary>
    /// <param name="storefront">The storefront entity with updated values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    void Update(Storefront storefront);

    /// <summary>
    /// Deletes a storefront from the repository.
    /// </summary>
    /// <param name="storefrontId">The storefront ID to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteAsync(Guid storefrontId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes to the repository.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SaveAsync(CancellationToken cancellationToken = default);
}
