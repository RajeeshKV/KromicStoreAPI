// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Domain.Entities;

using System;

/// <summary>
/// Represents tenant usage metrics for quota tracking.
/// </summary>
public class TenantUsage : BaseEntity
{
    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the usage type (Storage, ApiCalls, Bandwidth, Users, etc.).
    /// </summary>
    public string UsageType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the usage amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the unit of measurement (GB, Count, MB, etc.).
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the period start date.
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Gets or sets the period end date.
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Factory method to create a new usage record.
    /// </summary>
    public static TenantUsage Create(
        Guid tenantId,
        string usageType,
        decimal amount,
        string unit,
        DateTime periodStart,
        DateTime periodEnd)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(usageType))
            throw new ArgumentException("Usage type is required.", nameof(usageType));

        return new TenantUsage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UsageType = usageType,
            Amount = amount,
            Unit = unit,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Increments the usage amount.
    /// </summary>
    public void Increment(decimal amount)
    {
        Amount += amount;
        UpdateTimestamp();
    }
}
