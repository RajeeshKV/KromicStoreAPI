namespace KromicStore.Infrastructure.Data.Repositories;

using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

/// <summary>
/// Repository implementation for managing unified themes (platform and tenant-specific).
/// </summary>
public class ThemeRepository : Repository<Theme>, IThemeRepository
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
    public new async Task<Theme?> GetByIdAsync(
        Guid themeId,
        CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            return await _context.Themes
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == themeId, cancellationToken);
        }
        finally
        {
            watch.Stop();
            if (watch.ElapsedMilliseconds > SlowQueryThresholdMs)
            {
                _logger.LogWarning(
                    "Slow query detected for ThemeRepository.GetByIdAsync: {DurationMs}ms",
                    watch.ElapsedMilliseconds);
            }
        }
    }

    /// <inheritdoc />
    public async Task<Theme?> GetBySlugAsync(
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
    public async Task<List<Theme>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            return await _context.Themes
                .AsNoTracking()
                .Where(t => t.IsActive && t.OwnerTenantId == null) // Platform themes only
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
    public async Task<List<Theme>> GetAvailableForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            // Get platform themes + tenant's own themes + public themes from other tenants
            return await _context.Themes
                .AsNoTracking()
                .Where(t => 
                    (t.OwnerTenantId == null && t.IsActive) ||  // Platform themes
                    (t.OwnerTenantId == tenantId && t.IsActive) ||  // Tenant's own themes
                    (t.IsPublic && t.IsActive))  // Public themes from other tenants
                .OrderBy(t => t.OwnerTenantId == null ? 0 : 1)  // Platform themes first
                .ThenBy(t => t.Name)
                .ToListAsync(cancellationToken);
        }
        finally
        {
            watch.Stop();
            if (watch.ElapsedMilliseconds > SlowQueryThresholdMs)
            {
                _logger.LogWarning(
                    "Slow query detected for ThemeRepository.GetAvailableForTenantAsync: {DurationMs}ms",
                    watch.ElapsedMilliseconds);
            }
        }
    }

    /// <inheritdoc />
    public async Task<List<Theme>> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            return await _context.Themes
                .AsNoTracking()
                .Where(t => t.OwnerTenantId == tenantId)
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);
        }
        finally
        {
            watch.Stop();
            if (watch.ElapsedMilliseconds > SlowQueryThresholdMs)
            {
                _logger.LogWarning(
                    "Slow query detected for ThemeRepository.GetByTenantAsync: {DurationMs}ms",
                    watch.ElapsedMilliseconds);
            }
        }
    }

    /// <inheritdoc />
    public new async Task AddAsync(Theme entity, CancellationToken cancellationToken = default)
    {
        await _context.Themes.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public new void Update(Theme entity)
    {
        _context.Themes.Update(entity);
    }

    /// <inheritdoc />
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
