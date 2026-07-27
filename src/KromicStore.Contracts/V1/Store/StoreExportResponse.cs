// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Contracts.V1.Store;

/// <summary>
/// Response DTO for store export.
/// </summary>
public class StoreExportResponse
{
    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the export date.
    /// </summary>
    public DateTime ExportDate { get; set; }

    /// <summary>
    /// Gets or sets the exported data as JSON.
    /// </summary>
    public string Data { get; set; } = string.Empty;
}
