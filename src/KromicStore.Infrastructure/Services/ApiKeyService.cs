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
/// Implementation of API key service.
/// </summary>
public class ApiKeyService : IApiKeyService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ApiKeyService> _logger;

    public ApiKeyService(AppDbContext context, ILogger<ApiKeyService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(ApiKey apiKey, string plainKey)> CreateApiKeyAsync(
        Guid tenantId,
        string name,
        string scopes,
        Guid createdBy,
        DateTime? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        var apiKey = ApiKey.Create(tenantId, name, scopes, createdBy, expiresAt);
        var plainKey = ApiKey.GenerateApiKey().key;
        
        await _context.ApiKeys.AddAsync(apiKey, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "API key created for tenant {TenantId}: {Name}",
            tenantId, name);

        return (apiKey, plainKey);
    }

    public async Task<IEnumerable<ApiKey>> GetTenantApiKeysAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ApiKeys
            .Where(k => k.TenantId == tenantId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ApiKey?> GetApiKeyAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == id, cancellationToken);
    }

    public async Task<Guid?> ValidateApiKeyAsync(
        string plainKey,
        CancellationToken cancellationToken = default)
    {
        var keyHash = ApiKey.Hash(plainKey);
        
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash, cancellationToken);

        if (apiKey == null || !apiKey.IsValid())
        {
            return null;
        }

        // Update last used timestamp
        apiKey.UpdateLastUsed();
        await _context.SaveChangesAsync(cancellationToken);

        return apiKey.TenantId;
    }

    public async Task RevokeApiKeyAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == id, cancellationToken);

        if (apiKey == null)
            throw new ArgumentException("API key not found", nameof(id));

        apiKey.Revoke();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("API key {Id} revoked", id);
    }

    public async Task UpdateLastUsedAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == id, cancellationToken);

        if (apiKey == null)
            throw new ArgumentException("API key not found", nameof(id));

        apiKey.UpdateLastUsed();
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CleanupExpiredKeysAsync(CancellationToken cancellationToken = default)
    {
        var expiredKeys = await _context.ApiKeys
            .Where(k => k.IsActive && k.IsExpired())
            .ToListAsync(cancellationToken);

        foreach (var key in expiredKeys)
        {
            key.Revoke();
        }

        if (expiredKeys.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Cleaned up {Count} expired API keys", expiredKeys.Count);
        }
    }
}
