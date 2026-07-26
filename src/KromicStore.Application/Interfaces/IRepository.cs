namespace KromicStore.Application.Interfaces;

using Domain.Entities;
using System.Linq.Expressions;

/// <summary>
/// Generic repository interface for database operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    /// <summary>
    /// Retrieves an entity by its ID.
    /// </summary>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities.
    /// </summary>
    Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds entities matching the specified predicate.
    /// </summary>
    Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity.
    /// </summary>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    void Update(TEntity entity);

    /// <summary>
    /// Deletes an entity.
    /// </summary>
    void Delete(TEntity entity);

    /// <summary>
    /// Checks if any entity matches the predicate.
    /// </summary>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching the predicate.
    /// </summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);
}
