namespace KromicStore.Infrastructure.Data.Repositories;

using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

/// <summary>
/// Repository implementation for managing themes.
/// </summary>
public class ThemeRepository : Repository<ThemeEntity>, IThemeRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<ThemeRepository> _logger;

    /// <summary>
    /// Slow query threshold in milliseconds.
    /// </summary>
    private const int SlowQueryThresholdMs = 500;

    /// <summary>
    /// Initializes a new instance of the ThemeRepository class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    public ThemeRepository(AppDbContext context, ILogger<ThemeRepository> logger)
        : base(context, logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ThemeEntity?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug cannot be empty.", nameof(slug));

        var watch = Stopwatch.StartNew();
        try
        {
            var normalizedSlug = slug.ToLowerInvariant();
            return await _context.Themes
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Slug == normalizedSlug, cancellationToken);
        }
        finally
        {
            watch.Stop();
            if (watch.ElapsedMilliseconds > SlowQueryThresholdMs)
            {
                _logger.LogWarning(
                    "Slow query detected for ThemeRepository.GetBySlugAsync: {DurationMs}ms",
                    watch.ElapsedMilliseconds);
            }
        }
    }

    /// <inheritdoc />
    public async Task<List<ThemeEntity>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            return await _context.Themes
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);
        }
        finally
        {
            watch.Stop();
            if (watch.ElapsedMilliseconds > SlowQueryThresholdMs)
            {
                _logger.LogWarning(
                    "Slow query detected for ThemeRepository.GetActiveAsync: {DurationMs}ms",
                    watch.ElapsedMilliseconds);
            }
        }
    }

    /// <inheritdoc />
    public new void Update(ThemeEntity entity)
    {
        _context.Themes.Update(entity);
    }

    /// <inheritdoc />
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
