// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Infrastructure.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KromicStore.Application.Interfaces;
using KromicStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of SuperUser analytics dashboard service.
/// </summary>
public class SuperUserAnalyticsService : ISuperUserAnalyticsService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SuperUserAnalyticsService> _logger;

    public SuperUserAnalyticsService(AppDbContext context, ILogger<SuperUserAnalyticsService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PlatformAnalytics> GetPlatformAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var totalTenants = await _context.Tenants.CountAsync(cancellationToken);
        var activeTenants = await _context.Tenants.CountAsync(t => t.IsActive, cancellationToken);
        var newTenantsThisMonth = await _context.Tenants
            .CountAsync(t => t.CreatedAt >= monthStart, cancellationToken);

        var tenantsByPlan = await _context.Tenants
            .GroupBy(t => t.SubscriptionPlan)
            .Select(g => new { Plan = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Plan, g => g.Count, cancellationToken);

        var tenantsByStatus = new Dictionary<string, int>
        {
            { "Active", activeTenants },
            { "Suspended", await _context.Tenants.CountAsync(t => !t.IsActive && !t.IsDeleted, cancellationToken) },
            { "Deleted", await _context.Tenants.CountAsync(t => t.IsDeleted, cancellationToken) }
        };

        // Generate trend data (last 6 months)
        var revenueTrend = new List<TrendData>();
        var tenantGrowthTrend = new List<TrendData>();
        
        for (int i = 5; i >= 0; i--)
        {
            var trendDate = now.AddMonths(-i);
            var trendMonthStart = new DateTime(trendDate.Year, trendDate.Month, 1);
            
            revenueTrend.Add(new TrendData
            {
                Date = trendMonthStart,
                Value = await _context.Tenants
                    .Where(t => t.CreatedAt >= trendMonthStart && t.CreatedAt < trendMonthStart.AddMonths(1))
                    .CountAsync(cancellationToken) * 100 // Mock revenue calculation
            });
            
            tenantGrowthTrend.Add(new TrendData
            {
                Date = trendMonthStart,
                Value = await _context.Tenants
                    .Where(t => t.CreatedAt >= trendMonthStart && t.CreatedAt < trendMonthStart.AddMonths(1))
                    .CountAsync(cancellationToken)
            });
        }

        return new PlatformAnalytics
        {
            TotalTenants = totalTenants,
            ActiveTenants = activeTenants,
            NewTenantsThisMonth = newTenantsThisMonth,
            MonthlyRecurringRevenue = activeTenants * 100, // Mock calculation
            RevenueThisMonth = newTenantsThisMonth * 100, // Mock calculation
            TenantsByPlan = tenantsByPlan,
            TenantsByStatus = tenantsByStatus,
            RevenueTrend = revenueTrend,
            TenantGrowthTrend = tenantGrowthTrend
        };
    }

    public async Task<IEnumerable<TenantHealthMetrics>> GetTenantHealthMetricsAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _context.Tenants
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        var metrics = new List<TenantHealthMetrics>();

        foreach (var tenant in tenants)
        {
            var storageUsed = await _context.TenantUsage
                .Where(u => u.TenantId == tenant.Id && u.UsageType == "Storage")
                .SumAsync(u => u.Amount, cancellationToken);

            var apiCalls = (long)await _context.TenantUsage
                .Where(u => u.TenantId == tenant.Id && u.UsageType == "ApiCalls")
                .SumAsync(u => u.Amount, cancellationToken);

            var issues = new List<string>();
            var healthScore = "Good";

            if (storageUsed > 10) // 10 GB threshold
            {
                issues.Add("High storage usage");
                healthScore = "Warning";
            }

            if (apiCalls > 10000) // 10k API calls threshold
            {
                issues.Add("High API usage");
                healthScore = "Warning";
            }

            if (tenant.SuspendedAt.HasValue)
            {
                issues.Add("Tenant suspended");
                healthScore = "Critical";
            }

            metrics.Add(new TenantHealthMetrics
            {
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                Status = tenant.IsActive ? "Active" : "Inactive",
                StorageUsed = storageUsed,
                ApiCallsThisMonth = apiCalls,
                ActiveUsers = 0, // Would need to track this separately
                LastActivity = tenant.UpdatedAt,
                HealthScore = healthScore,
                Issues = issues
            });
        }

        return metrics;
    }

    public async Task<SystemPerformanceMetrics> GetSystemPerformanceMetricsAsync(CancellationToken cancellationToken = default)
    {
        // In production, this would query actual system metrics
        // For now, return mock data
        await Task.CompletedTask;

        return new SystemPerformanceMetrics
        {
            CpuUsage = 45.5,
            MemoryUsage = 62.3,
            DiskUsage = 78.1,
            NetworkIn = 1250.5,
            NetworkOut = 890.2,
            AverageResponseTime = 145.5,
            RequestsPerSecond = 125,
            ErrorRate = 0.02,
            CollectedAt = DateTime.UtcNow
        };
    }

    public async Task<IEnumerable<SecurityAlert>> GetSecurityAlertsAsync(CancellationToken cancellationToken = default)
    {
        // In production, this would query actual security logs
        // For now, return mock data based on audit logs
        var recentFailedLogins = await _context.AuditLogs
            .Where(a => a.Action == "Login" && !a.Success && a.OccurredAt >= DateTime.UtcNow.AddDays(-7))
            .OrderByDescending(a => a.OccurredAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        var alerts = new List<SecurityAlert>();

        foreach (var failedLogin in recentFailedLogins)
        {
            alerts.Add(new SecurityAlert
            {
                Id = Guid.NewGuid(),
                Type = "FailedLogin",
                Severity = "Medium",
                Description = $"Failed login attempt from IP {failedLogin.IpAddress}",
                TenantId = failedLogin.TenantId,
                OccurredAt = failedLogin.OccurredAt,
                IsResolved = false
            });
        }

        return alerts;
    }
}
