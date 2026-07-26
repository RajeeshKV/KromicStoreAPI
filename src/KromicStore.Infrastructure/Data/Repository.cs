namespace KromicStore.Infrastructure.Data;

using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq.Expressions;

/// <summary>
/// Generic repository implementation with query optimization patterns.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class Repository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    private readonly AppDbContext _context;
    private readonly DbSet<TEntity> _dbSet;
    private readonly ILogger<Repository<TEntity>> _logger;

    /// <summary>
    /// Maximum items allowed per page for pagination.
    /// </summary>
    private const int MaxPageSize = 100;

    /// <summary>
    /// Slow query threshold in milliseconds.
    /// </summary>
    private const int SlowQueryThresholdMs = 500;

    /// <summary>
    /// Initializes a new instance of the Repository class.
    /// </summary>
    public Repository(AppDbContext context, ILogger<Repository<TEntity>> logger)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            return await _dbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }
        finally
        {
            watch.Stop();
            if (watch.ElapsedMilliseconds > SlowQueryThresholdMs)
            {
                _logger.LogWarning(
                    "Slow query detected for {EntityType}.GetByIdAsync: {DurationMs}ms",
                    typeof(TEntity).Name,
                    watch.ElapsedMilliseconds);
            }
        }
    }

    /// <inheritdoc />
    public async Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            return await _dbSet.ToListAsync(cancellationToken);
        }
        finally
        {
            watch.Stop();
            if (watch.ElapsedMilliseconds > SlowQueryThresholdMs)
            {
                _logger.LogWarning(
                    "Slow query detected for {EntityType}.GetAllAsync: {DurationMs}ms",
                    typeof(TEntity).Name,
                    watch.ElapsedMilliseconds);
            }
        }
    }

    /// <inheritdoc />
    public async Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
        }
        finally
        {
            watch.Stop();
            if (watch.ElapsedMilliseconds > SlowQueryThresholdMs)
            {
                _logger.LogWarning(
                    "Slow query detected for {EntityType}.FindAsync: {DurationMs}ms",
                    typeof(TEntity).Name,
                    watch.ElapsedMilliseconds);
            }
        }
    }

    /// <summary>
    /// Retrieves paginated results with optional projection.
    /// </summary>
    /// <typeparam name="TDto">The DTO type for projection.</typeparam>
    /// <param name="predicate">Filter condition.</param>
    /// <param name="selector">Projection selector.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated results with metadata.</returns>
    public async Task<(List<TDto> Items, int TotalCount)> GetPaginatedAsync<TDto>(
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, TDto>>? selector = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default) where TDto : class
    {
        // Enforce max page size
        pageSize = Math.Min(pageSize, MaxPageSize);
        if (pageNumber < 1) pageNumber = 1;

        var watch = Stopwatch.StartNew();
        try
        {
            var query = _dbSet.AsNoTracking();

            if (predicate != null)
                query = query.Where(predicate);

            var totalCount = await query.CountAsync(cancellationToken);

            var skip = (pageNumber - 1) * pageSize;
            var items = selector == null
                ? await query
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false) as List<TDto>
                : await query
                    .Select(selector)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

            return (items ?? new List<TDto>(), totalCount);
        }
        finally
        {
            watch.Stop();
            if (watch.ElapsedMilliseconds > SlowQueryThresholdMs)
            {
                _logger.LogWarning(
                    "Slow paginated query detected for {EntityType}: Page {PageNumber}, Size {PageSize}, {DurationMs}ms",
                    typeof(TEntity).Name,
                    pageNumber,
                    pageSize,
                    watch.ElapsedMilliseconds);
            }
        }
    }

    /// <summary>
    /// Retrieves results with deferred projection (IQueryable composition).
    /// </summary>
    /// <param name="predicate">Filter condition.</param>
    /// <returns>Queryable sequence for composition.</returns>
    public IQueryable<TEntity> AsQueryable(Expression<Func<TEntity, bool>>? predicate = null)
    {
        var query = _dbSet.AsNoTracking();
        return predicate != null ? query.Where(predicate) : query;
    }

    /// <summary>
    /// Performs full-text search on the specified properties.
    /// </summary>
    /// <param name="searchTerm">Search term.</param>
    /// <param name="searchProperties">Property selectors to search.</param>
    /// <param name="pageNumber">Page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results with pagination metadata.</returns>
    public async Task<(List<TEntity> Items, int TotalCount)> FullTextSearchAsync(
        string searchTerm,
        Expression<Func<TEntity, string>>[] searchProperties,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return (new List<TEntity>(), 0);

        pageSize = Math.Min(pageSize, MaxPageSize);
        if (pageNumber < 1) pageNumber = 1;

        var watch = Stopwatch.StartNew();
        try
        {
            var query = _dbSet.AsNoTracking();

            // Build OR expression for multiple properties
            var searchLower = searchTerm.ToLower();
            Expression<Func<TEntity, bool>>? searchPredicate = null;

            foreach (var property in searchProperties)
            {
                var stringProperty = Expression.Call(
                    property.Body,
                    "ToLower",
                    null);

                var contains = Expression.Call(
                    stringProperty,
                    "Contains",
                    null,
                    Expression.Constant(searchLower));

                var lambda = Expression.Lambda<Func<TEntity, bool>>(contains, property.Parameters);

                searchPredicate = searchPredicate == null
                    ? lambda
                    : CombineWithOr(searchPredicate, lambda);
            }

            if (searchPredicate != null)
                query = query.Where(searchPredicate);

            var totalCount = await query.CountAsync(cancellationToken);
            var skip = (pageNumber - 1) * pageSize;

            var items = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
        finally
        {
            watch.Stop();
            if (watch.ElapsedMilliseconds > SlowQueryThresholdMs)
            {
                _logger.LogWarning(
                    "Slow full-text search for {EntityType} with term '{SearchTerm}': {DurationMs}ms",
                    typeof(TEntity).Name,
                    searchTerm,
                    watch.ElapsedMilliseconds);
            }
        }
    }

    /// <inheritdoc />
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(TEntity entity)
    {
        _dbSet.Update(entity);
    }

    /// <inheritdoc />
    public void Delete(TEntity entity)
    {
        _dbSet.Remove(entity);
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            return await _dbSet.AnyAsync(predicate, cancellationToken);
        }
        finally
        {
            watch.Stop();
            if (watch.ElapsedMilliseconds > SlowQueryThresholdMs)
            {
                _logger.LogWarning(
                    "Slow query detected for {EntityType}.AnyAsync: {DurationMs}ms",
                    typeof(TEntity).Name,
                    watch.ElapsedMilliseconds);
            }
        }
    }

    /// <inheritdoc />
    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            return predicate == null
                ? await _dbSet.CountAsync(cancellationToken)
                : await _dbSet.CountAsync(predicate, cancellationToken);
        }
        finally
        {
            watch.Stop();
            if (watch.ElapsedMilliseconds > SlowQueryThresholdMs)
            {
                _logger.LogWarning(
                    "Slow query detected for {EntityType}.CountAsync: {DurationMs}ms",
                    typeof(TEntity).Name,
                    watch.ElapsedMilliseconds);
            }
        }
    }

    /// <summary>
    /// Combines two predicates with OR operator.
    /// </summary>
    private static Expression<Func<TEntity, bool>> CombineWithOr(
        Expression<Func<TEntity, bool>> expr1,
        Expression<Func<TEntity, bool>> expr2)
    {
        var parameter = expr1.Parameters[0];
        var body = Expression.OrElse(expr1.Body, Expression.Invoke(expr2, parameter));
        return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
    }
}
