// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Application.Interfaces;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service interface for store export and cloning operations.
/// </summary>
public interface IStoreExportService
{
    /// <summary>
    /// Exports store data to a JSON format.
    /// </summary>
    Task<string> ExportStoreAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports store data from JSON format.
    /// </summary>
    Task ImportStoreAsync(
        Guid tenantId,
        string jsonData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clones a store to a new tenant.
    /// </summary>
    Task<Guid> CloneStoreAsync(
        Guid sourceTenantId,
        Guid targetTenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a backup snapshot of the store.
    /// </summary>
    Task<Guid> CreateBackupAsync(
        Guid tenantId,
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a store from a backup.
    /// </summary>
    Task RestoreFromBackupAsync(
        Guid tenantId,
        Guid backupId,
        CancellationToken cancellationToken = default);
}
