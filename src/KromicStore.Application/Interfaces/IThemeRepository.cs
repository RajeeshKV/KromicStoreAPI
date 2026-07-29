namespace KromicStore.Application.Interfaces;

using KromicStore.Domain.Entities;

/// <summary>
/// Repository interface for managing themes (unified platform and tenant-specific themes).
/// </summary>
public interface IThemeRepository
{
    /// <summary>
    /// Retrieves a theme by its unique identifier.
    /// </summary>
    /// <param name="themeId">The theme ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The theme if found; otherwise null.</returns>
    Task<Theme?> GetByIdAsync(Guid themeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a theme by its slug (unique identifier string).
    /// </summary>
    /// <param name="slug">The theme slug (e.g., "minimal", "modern", "pro").</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The theme if found; otherwise null.</returns>
    Task<Theme?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active themes (platform and public tenant themes).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of active themes.</returns>
    Task<List<Theme>> GetActiveAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves all themes available to a tenant (platform themes + tenant's own + public themes).
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of themes available to the tenant.</returns>
    Task<List<Theme>> GetAvailableForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves all themes owned by a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of themes owned by the tenant.</returns>
    Task<List<Theme>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new theme to the repository.
    /// </summary>
    /// <param name="theme">The theme to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AddAsync(Theme theme, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing theme in the repository.
    /// </summary>
    /// <param name="theme">The theme with updated values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    void Update(Theme theme);

    /// <summary>
    /// Saves all pending changes to the repository.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SaveAsync(CancellationToken cancellationToken = default);
}
