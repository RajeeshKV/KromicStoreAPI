// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Contracts.V1.Store;

/// <summary>
/// Response DTO for creating a store backup.
/// </summary>
public class CreateBackupResponse
{
    /// <summary>
    /// Gets or sets the backup ID.
    /// </summary>
    public Guid BackupId { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the backup description.
    /// </summary>
    public string? Description { get; set; }
}
