// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Contracts.V1.Tenants;

/// <summary>
/// Response DTO for tenant lifecycle operations.
/// </summary>
public class TenantLifecycleResponse
{
    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the current status (Active, Suspended, Archived, SoftDeleted).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the tenant is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets whether the tenant is archived.
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Gets or sets whether the tenant is soft deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the date when the tenant was suspended.
    /// </summary>
    public DateTime? SuspendedAt { get; set; }

    /// <summary>
    /// Gets or sets the date when the tenant was archived.
    /// </summary>
    public DateTime? ArchivedAt { get; set; }

    /// <summary>
    /// Gets or sets the date when the tenant was soft deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the reason for the lifecycle change.
    /// </summary>
    public string? Reason { get; set; }
}
