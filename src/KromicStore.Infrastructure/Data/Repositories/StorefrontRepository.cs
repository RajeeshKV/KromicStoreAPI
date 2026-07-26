namespace KromicStore.Infrastructure.Data.Repositories;

using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

/// <summary>
/// Repository implementation for managing storefronts with their related components.
/// </summary>
public class StorefrontRepository : Repository<Storefront>, IStorefrontRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<StorefrontRepository> _logger;

    /// <summary>
    /// Slow query threshold in milliseconds.
    /// </summary>
    private const int SlowQueryThresholdMs = 500;

    /// <summary>
    /// Initializes a new instance of the StorefrontRepository class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    public StorefrontRepository(AppDbContext context, ILogger<StorefrontRepository> logger)
        : base(context, logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Storefront?> GetByIdAsync(
        Guid storefrontId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            return await _context.Storefronts
                .AsNoTracking()
                .Include(s => s.Pages)
                    .ThenInclude(p => p.Sections)
                    .ThenInclude(sec => sec.Components)
                .FirstOrDefaultAsync(
                    s => s.Id == storefrontId && s.TenantId == tenantId,
                    cancellationToken);
        }
        finally
        {
            watch.Stop();
            if (watch.ElapsedMilliseconds > SlowQueryThresholdMs)
            {
                _logger.LogWarning(
                    "Slow query detected for StorefrontRepository.GetByIdAsync: {DurationMs}ms",
                    watch.ElapsedMilliseconds);
            }
        }
    }

    /// <inheritdoc />
    public async Task<List<Storefront>> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            return await _context.Storefronts
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId)
                .Include(s => s.Pages)
                    .ThenInclude(p => p.Sections)
                    .ThenInclude(sec => sec.Components)
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);
        }
        finally
        {
            watch.Stop();
            if (watch.ElapsedMilliseconds > SlowQueryThresholdMs)
            {
                _logger.LogWarning(
                    "Slow query detected for StorefrontRepository.GetByTenantAsync: {DurationMs}ms",
                    watch.ElapsedMilliseconds);
            }
        }
    }

    /// <inheritdoc />
    public new void Update(Storefront entity)
    {
        _context.Storefronts.Update(entity);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid storefrontId, CancellationToken cancellationToken = default)
    {
        var storefront = await _context.Storefronts
            .FirstOrDefaultAsync(s => s.Id == storefrontId, cancellationToken);

        if (storefront != null)
        {
            _context.Storefronts.Remove(storefront);
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
