// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Application.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service interface for SuperUser analytics dashboard.
/// </summary>
public interface ISuperUserAnalyticsService
{
    /// <summary>
    /// Gets platform-wide analytics dashboard data.
    /// </summary>
    Task<PlatformAnalytics> GetPlatformAnalyticsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tenant health metrics.
    /// </summary>
    Task<IEnumerable<TenantHealthMetrics>> GetTenantHealthMetricsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets system performance metrics.
    /// </summary>
    Task<SystemPerformanceMetrics> GetSystemPerformanceMetricsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets security alerts and incidents.
    /// </summary>
    Task<IEnumerable<SecurityAlert>> GetSecurityAlertsAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Platform-wide analytics data.
/// </summary>
public class PlatformAnalytics
{
    public int TotalTenants { get; set; }
    public int ActiveTenants { get; set; }
    public int NewTenantsThisMonth { get; set; }
    public decimal MonthlyRecurringRevenue { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public Dictionary<string, int> TenantsByPlan { get; set; } = new();
    public Dictionary<string, int> TenantsByStatus { get; set; } = new();
    public List<TrendData> RevenueTrend { get; set; } = new();
    public List<TrendData> TenantGrowthTrend { get; set; } = new();
}

/// <summary>
/// Tenant health metrics.
/// </summary>
public class TenantHealthMetrics
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal StorageUsed { get; set; }
    public long ApiCallsThisMonth { get; set; }
    public int ActiveUsers { get; set; }
    public DateTime LastActivity { get; set; }
    public string HealthScore { get; set; } = string.Empty;
    public List<string> Issues { get; set; } = new();
}

/// <summary>
/// System performance metrics.
/// </summary>
public class SystemPerformanceMetrics
{
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double DiskUsage { get; set; }
    public double NetworkIn { get; set; }
    public double NetworkOut { get; set; }
    public double AverageResponseTime { get; set; }
    public int RequestsPerSecond { get; set; }
    public double ErrorRate { get; set; }
    public DateTime CollectedAt { get; set; }
}

/// <summary>
/// Security alert.
/// </summary>
public class SecurityAlert
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public DateTime OccurredAt { get; set; }
    public bool IsResolved { get; set; }
}

/// <summary>
/// Trend data point.
/// </summary>
public class TrendData
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
}
