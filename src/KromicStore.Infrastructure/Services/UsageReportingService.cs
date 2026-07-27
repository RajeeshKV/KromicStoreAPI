// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Infrastructure.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KromicStore.Application.Interfaces;
using KromicStore.Domain.Entities;
using KromicStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of usage reporting and quota management service.
/// </summary>
public class UsageReportingService : IUsageReportingService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UsageReportingService> _logger;

    public UsageReportingService(AppDbContext context, ILogger<UsageReportingService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RecordUsageAsync(
        Guid tenantId,
        string usageType,
        decimal amount,
        string unit,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        var existingUsage = await _context.TenantUsage
            .FirstOrDefaultAsync(u => 
                u.TenantId == tenantId 
                && u.UsageType == usageType 
                && u.PeriodStart == periodStart 
                && u.PeriodEnd == periodEnd,
                cancellationToken);

        if (existingUsage != null)
        {
            existingUsage.Increment(amount);
        }
        else
        {
            var newUsage = TenantUsage.Create(
                tenantId,
                usageType,
                amount,
                unit,
                periodStart,
                periodEnd);
            await _context.TenantUsage.AddAsync(newUsage, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Usage recorded for tenant {TenantId}: {Type} - {Amount} {Unit}",
            tenantId, usageType, amount, unit);
    }

    public async Task<IEnumerable<TenantUsage>> GetUsageAsync(
        Guid tenantId,
        DateTime from,
        DateTime to,
        string? usageType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TenantUsage
            .Where(u => u.TenantId == tenantId 
                && u.PeriodStart >= from 
                && u.PeriodEnd <= to);

        if (!string.IsNullOrWhiteSpace(usageType))
        {
            query = query.Where(u => u.UsageType == usageType);
        }

        return await query
            .OrderBy(u => u.PeriodStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<UsageSummary> GetUsageSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        var currentUsage = await _context.TenantUsage
            .Where(u => u.TenantId == tenantId 
                && u.PeriodStart == periodStart 
                && u.PeriodEnd == periodEnd)
            .ToListAsync(cancellationToken);

        var summary = new UsageSummary
        {
            CurrentUsage = currentUsage.ToDictionary(u => u.UsageType, u => u.Amount),
            Quotas = GetDefaultQuotas(),
            QuotaExceeded = new Dictionary<string, bool>()
        };

        foreach (var usage in currentUsage)
        {
            if (summary.Quotas.TryGetValue(usage.UsageType, out var quota))
            {
                summary.QuotaExceeded[usage.UsageType] = usage.Amount > quota;
            }
        }

        return summary;
    }

    public async Task<bool> CheckQuotaExceededAsync(
        Guid tenantId,
        string usageType,
        CancellationToken cancellationToken = default)
    {
        var summary = await GetUsageSummaryAsync(tenantId, cancellationToken);
        return summary.QuotaExceeded.GetValueOrDefault(usageType, false);
    }

    public async Task<PlatformUsageStats> GetPlatformUsageStatsAsync(
        CancellationToken cancellationToken = default)
    {
        var totalTenants = await _context.Tenants.CountAsync(cancellationToken);
        var activeTenants = await _context.Tenants
            .CountAsync(t => t.IsActive, cancellationToken);

        var totalStorageUsed = await _context.TenantUsage
            .Where(u => u.UsageType == "Storage")
            .SumAsync(u => u.Amount, cancellationToken);

        var totalApiCalls = (long)await _context.TenantUsage
            .Where(u => u.UsageType == "ApiCalls")
            .SumAsync(u => u.Amount, cancellationToken);

        var tenantsByPlan = await _context.Tenants
            .GroupBy(t => t.SubscriptionPlan)
            .Select(g => new { Plan = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Plan, g => g.Count, cancellationToken);

        return new PlatformUsageStats
        {
            TotalTenants = totalTenants,
            ActiveTenants = activeTenants,
            TotalStorageUsed = totalStorageUsed,
            TotalApiCalls = totalApiCalls,
            TotalRevenue = 0, // Calculate from subscriptions
            TenantByPlan = tenantsByPlan
        };
    }

    private Dictionary<string, decimal> GetDefaultQuotas()
    {
        return new Dictionary<string, decimal>
        {
            { "Storage", 10 }, // GB
            { "ApiCalls", 10000 }, // Count per month
            { "Bandwidth", 100 }, // GB per month
            { "Users", 5 } // Count
        };
    }
}
