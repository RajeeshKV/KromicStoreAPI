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
/// Implementation of feature flag service.
/// </summary>
public class FeatureFlagService : IFeatureFlagService
{
    private readonly AppDbContext _context;
    private readonly ILogger<FeatureFlagService> _logger;

    public FeatureFlagService(AppDbContext context, ILogger<FeatureFlagService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> IsFeatureEnabledAsync(
        Guid tenantId,
        string featureKey,
        CancellationToken cancellationToken = default)
    {
        // Check tenant-specific flag first
        var tenantFlag = await _context.FeatureFlags
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.Key == featureKey, cancellationToken);

        if (tenantFlag != null)
            return tenantFlag.IsEnabled;

        // Check plan-based flags (if tenant has a plan)
        // For now, just check global flags
        var globalFlag = await _context.FeatureFlags
            .FirstOrDefaultAsync(f => f.TenantId == null && f.Key == featureKey, cancellationToken);

        return globalFlag?.IsEnabled ?? false;
    }

    public async Task<IEnumerable<FeatureFlag>> GetTenantFeatureFlagsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenantFlags = await _context.FeatureFlags
            .Where(f => f.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        // Also include global flags that don't have tenant-specific overrides
        var globalFlags = await _context.FeatureFlags
            .Where(f => f.TenantId == null)
            .ToListAsync(cancellationToken);

        var globalFlagKeys = globalFlags.Select(f => f.Key).ToHashSet();
        var tenantFlagKeys = tenantFlags.Select(f => f.Key).ToHashSet();

        // Include global flags that don't have tenant overrides
        var flagsToInclude = globalFlags
            .Where(f => !tenantFlagKeys.Contains(f.Key))
            .ToList();

        return tenantFlags.Concat(flagsToInclude);
    }

    public async Task<FeatureFlag?> GetFeatureFlagAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.FeatureFlags
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<FeatureFlag> CreateFeatureFlagAsync(
        Guid? tenantId,
        string key,
        bool isEnabled,
        string? description = null,
        string type = "Tenant",
        string? plan = null,
        CancellationToken cancellationToken = default)
    {
        var featureFlag = FeatureFlag.Create(tenantId, key, isEnabled, description, type, plan);
        await _context.FeatureFlags.AddAsync(featureFlag, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Feature flag created: {Key} for tenant {TenantId}",
            key, tenantId);

        return featureFlag;
    }

    public async Task UpdateFeatureFlagAsync(
        Guid id,
        bool? isEnabled = null,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var featureFlag = await _context.FeatureFlags
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (featureFlag == null)
            throw new ArgumentException("Feature flag not found", nameof(id));

        if (isEnabled.HasValue)
        {
            if (isEnabled.Value)
                featureFlag.Enable();
            else
                featureFlag.Disable();
        }

        if (description != null)
        {
            featureFlag.Description = description;
            featureFlag.UpdateTimestamp();
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Feature flag {Id} updated", id);
    }

    public async Task DeleteFeatureFlagAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var featureFlag = await _context.FeatureFlags
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (featureFlag == null)
            throw new ArgumentException("Feature flag not found", nameof(id));

        _context.FeatureFlags.Remove(featureFlag);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Feature flag {Id} deleted", id);
    }

    public async Task<IEnumerable<FeatureFlag>> GetGlobalFeatureFlagsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.FeatureFlags
            .Where(f => f.TenantId == null)
            .ToListAsync(cancellationToken);
    }
}
