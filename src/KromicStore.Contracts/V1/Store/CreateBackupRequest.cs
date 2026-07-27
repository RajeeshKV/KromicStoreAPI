// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Contracts.V1.Store;

/// <summary>
/// Request DTO for creating a store backup.
/// </summary>
public class CreateBackupRequest
{
    /// <summary>
    /// Gets or sets the backup description.
    /// </summary>
    public string? Description { get; set; }
}
