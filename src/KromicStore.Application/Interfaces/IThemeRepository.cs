namespace KromicStore.Application.Interfaces;

using KromicStore.Domain.Entities;

/// <summary>
/// Repository interface for managing themes.
/// </summary>
public interface IThemeRepository
{
    /// <summary>
    /// Retrieves a theme by its unique identifier.
    /// </summary>
    /// <param name="themeId">The theme ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The theme entity if found; otherwise null.</returns>
    Task<ThemeEntity?> GetByIdAsync(Guid themeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a theme by its slug (unique identifier string).
    /// </summary>
    /// <param name="slug">The theme slug (e.g., "minimal", "modern", "pro").</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The theme entity if found; otherwise null.</returns>
    Task<ThemeEntity?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active themes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of active themes.</returns>
    Task<List<ThemeEntity>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new theme to the repository.
    /// </summary>
    /// <param name="theme">The theme entity to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AddAsync(ThemeEntity theme, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing theme in the repository.
    /// </summary>
    /// <param name="theme">The theme entity with updated values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    void Update(ThemeEntity theme);

    /// <summary>
    /// Saves all pending changes to the repository.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SaveAsync(CancellationToken cancellationToken = default);
}
