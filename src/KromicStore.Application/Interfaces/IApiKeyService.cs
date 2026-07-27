// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Application.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service interface for API key management.
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Creates a new API key for a tenant.
    /// </summary>
    Task<(Domain.Entities.ApiKey apiKey, string plainKey)> CreateApiKeyAsync(
        Guid tenantId,
        string name,
        string scopes,
        Guid createdBy,
        DateTime? expiresAt = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all API keys for a tenant.
    /// </summary>
    Task<IEnumerable<Domain.Entities.ApiKey>> GetTenantApiKeysAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an API key by ID.
    /// </summary>
    Task<Domain.Entities.ApiKey?> GetApiKeyAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an API key and returns the associated tenant ID.
    /// </summary>
    Task<Guid?> ValidateApiKeyAsync(
        string plainKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes an API key.
    /// </summary>
    Task RevokeApiKeyAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last used timestamp for an API key.
    /// </summary>
    Task UpdateLastUsedAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired API keys.
    /// </summary>
    Task CleanupExpiredKeysAsync(CancellationToken cancellationToken = default);
}
