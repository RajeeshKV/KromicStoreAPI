// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Domain.Entities;

using System;

/// <summary>
/// Represents a comprehensive audit log entry for all system actions.
/// Tracks changes across tenants, users, products, orders, and other entities.
/// </summary>
public class AuditLog : BaseEntity
{
    /// <summary>
    /// Gets or sets the tenant ID (null for platform-wide actions).
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who performed the action (null for system actions).
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the user type (User, SuperUser, System).
    /// </summary>
    public string? UserType { get; set; }

    /// <summary>
    /// Gets or sets the entity type that was affected (e.g., "Product", "Order", "Tenant").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the entity that was affected.
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the action performed (Create, Update, Delete, View, Export, etc.).
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the IP address from which the action was performed.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent string.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for request tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the old state before the change (JSON serialized).
    /// </summary>
    public string? OldState { get; set; }

    /// <summary>
    /// Gets or sets the new state after the change (JSON serialized).
    /// </summary>
    public string? NewState { get; set; }

    /// <summary>
    /// Gets or sets additional metadata or context about the action.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets whether this action was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the action failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the action occurred.
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Factory method to create a new AuditLog for a successful action.
    /// </summary>
    public static AuditLog Create(
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
        string? metadata = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            UserType = userType,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CorrelationId = correlationId,
            OldState = oldState,
            NewState = newState,
            Metadata = metadata,
            Success = true,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Factory method to create a new AuditLog for a failed action.
    /// </summary>
    public static AuditLog CreateFailure(
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
        string? metadata = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            UserType = userType,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CorrelationId = correlationId,
            Metadata = metadata,
            Success = false,
            ErrorMessage = errorMessage,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
