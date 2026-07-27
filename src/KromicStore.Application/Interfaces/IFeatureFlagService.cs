// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Application.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service interface for feature flag management.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Checks if a feature flag is enabled for a tenant.
    /// </summary>
    Task<bool> IsFeatureEnabledAsync(
        Guid tenantId,
        string featureKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all feature flags for a tenant.
    /// </summary>
    Task<IEnumerable<Domain.Entities.FeatureFlag>> GetTenantFeatureFlagsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific feature flag.
    /// </summary>
    Task<Domain.Entities.FeatureFlag?> GetFeatureFlagAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new feature flag.
    /// </summary>
    Task<Domain.Entities.FeatureFlag> CreateFeatureFlagAsync(
        Guid? tenantId,
        string key,
        bool isEnabled,
        string? description = null,
        string type = "Tenant",
        string? plan = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a feature flag.
    /// </summary>
    Task UpdateFeatureFlagAsync(
        Guid id,
        bool? isEnabled = null,
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a feature flag.
    /// </summary>
    Task DeleteFeatureFlagAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all global feature flags.
    /// </summary>
    Task<IEnumerable<Domain.Entities.FeatureFlag>> GetGlobalFeatureFlagsAsync(
        CancellationToken cancellationToken = default);
}
