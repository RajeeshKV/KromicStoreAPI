// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Application.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service interface for usage reporting and quota management.
/// </summary>
public interface IUsageReportingService
{
    /// <summary>
    /// Records usage for a tenant.
    /// </summary>
    Task RecordUsageAsync(
        Guid tenantId,
        string usageType,
        decimal amount,
        string unit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets usage for a tenant within a date range.
    /// </summary>
    Task<IEnumerable<Domain.Entities.TenantUsage>> GetUsageAsync(
        Guid tenantId,
        DateTime from,
        DateTime to,
        string? usageType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current usage summary for a tenant.
    /// </summary>
    Task<UsageSummary> GetUsageSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a tenant has exceeded their quota.
    /// </summary>
    Task<bool> CheckQuotaExceededAsync(
        Guid tenantId,
        string usageType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets platform-wide usage statistics (SuperUser only).
    /// </summary>
    Task<PlatformUsageStats> GetPlatformUsageStatsAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Usage summary for a tenant.
/// </summary>
public class UsageSummary
{
    public Dictionary<string, decimal> CurrentUsage { get; set; } = new();
    public Dictionary<string, decimal> Quotas { get; set; } = new();
    public Dictionary<string, bool> QuotaExceeded { get; set; } = new();
}

/// <summary>
/// Platform-wide usage statistics.
/// </summary>
public class PlatformUsageStats
{
    public int TotalTenants { get; set; }
    public int ActiveTenants { get; set; }
    public decimal TotalStorageUsed { get; set; }
    public long TotalApiCalls { get; set; }
    public decimal TotalRevenue { get; set; }
    public Dictionary<string, int> TenantByPlan { get; set; } = new();
}
