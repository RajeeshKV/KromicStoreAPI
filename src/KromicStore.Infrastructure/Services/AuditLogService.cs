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
/// Implementation of audit logging service.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(AppDbContext context, ILogger<AuditLogService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task LogActionAsync(
        Guid? tenantId,
        Guid? userId,
        string? userType,
        string entityType,
        Guid? entityId,
        string action,
        string? ipAddress = null,
        string? userAgent = null,
        string? correlationId = null,
        string? oldState = null,
        string? newState = null,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = AuditLog.Create(
                tenantId,
                userId,
                userType,
                entityType,
                entityId,
                action,
                ipAddress,
                userAgent,
                correlationId,
                oldState,
                newState,
                metadata);

            await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Audit log created: {EntityType} {EntityId} - {Action} by {UserId}",
                entityType,
                entityId,
                action,
                userId);
        }
        catch (Exception ex)
        {
            // Log failure but don't throw - audit logging shouldn't break the main operation
            _logger.LogError(ex, "Failed to create audit log for {EntityType} {EntityId} - {Action}", entityType, entityId, action);
        }
    }

    public async Task LogFailureAsync(
        Guid? tenantId,
        Guid? userId,
        string? userType,
        string entityType,
        Guid? entityId,
        string action,
        string errorMessage,
        string? ipAddress = null,
        string? userAgent = null,
        string? correlationId = null,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = AuditLog.CreateFailure(
                tenantId,
                userId,
                userType,
                entityType,
                entityId,
                action,
                errorMessage,
                ipAddress,
                userAgent,
                correlationId,
                metadata);

            await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Audit log (failure) created: {EntityType} {EntityId} - {Action} by {UserId}",
                entityType,
                entityId,
                action,
                userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log (failure) for {EntityType} {EntityId} - {Action}", entityType, entityId, action);
        }
    }

    public async Task<(IEnumerable<AuditLog> logs, int total)> GetTenantAuditLogsAsync(
        Guid tenantId,
        DateTime? from = null,
        DateTime? to = null,
        string? entityType = null,
        string? action = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs
            .Where(log => log.TenantId == tenantId);

        if (from.HasValue)
            query = query.Where(log => log.OccurredAt >= from.Value);

        if (to.HasValue)
            query = query.Where(log => log.OccurredAt <= to.Value);

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(log => log.EntityType == entityType);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(log => log.Action == action);

        var total = await query.CountAsync(cancellationToken);

        var logs = await query
            .OrderByDescending(log => log.OccurredAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (logs, total);
    }

    public async Task<IEnumerable<AuditLog>> GetEntityAuditLogsAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Where(log => log.EntityType == entityType && log.EntityId == entityId)
            .OrderByDescending(log => log.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetUserAuditLogsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<AuditLog> logs, int total)> GetPlatformAuditLogsAsync(
        DateTime? from = null,
        DateTime? to = null,
        string? entityType = null,
        string? action = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs
            .Where(log => log.TenantId == null); // Platform-wide logs

        if (from.HasValue)
            query = query.Where(log => log.OccurredAt >= from.Value);

        if (to.HasValue)
            query = query.Where(log => log.OccurredAt <= to.Value);

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(log => log.EntityType == entityType);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(log => log.Action == action);

        var total = await query.CountAsync(cancellationToken);

        var logs = await query
            .OrderByDescending(log => log.OccurredAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (logs, total);
    }
}
