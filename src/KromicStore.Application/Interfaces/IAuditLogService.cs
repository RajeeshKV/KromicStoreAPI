// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Application.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service interface for audit logging operations.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Logs a successful action.
    /// </summary>
    Task LogActionAsync(
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
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a failed action.
    /// </summary>
    Task LogFailureAsync(
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
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific tenant with pagination.
    /// </summary>
    Task<(IEnumerable<Domain.Entities.AuditLog> logs, int total)> GetTenantAuditLogsAsync(
        Guid tenantId,
        DateTime? from = null,
        DateTime? to = null,
        string? entityType = null,
        string? action = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific entity.
    /// </summary>
    Task<IEnumerable<Domain.Entities.AuditLog>> GetEntityAuditLogsAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific user.
    /// </summary>
    Task<IEnumerable<Domain.Entities.AuditLog>> GetUserAuditLogsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all platform-wide audit logs (SuperUser only).
    /// </summary>
    Task<(IEnumerable<Domain.Entities.AuditLog> logs, int total)> GetPlatformAuditLogsAsync(
        DateTime? from = null,
        DateTime? to = null,
        string? entityType = null,
        string? action = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);
}
