// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Domain.Enums;

/// <summary>
/// Represents the scope of a configuration setting.
/// </summary>
public enum ConfigScope
{
    /// <summary>
    /// Platform-wide configuration (SuperUser only).
    /// </summary>
    Platform = 0,

    /// <summary>
    /// Tenant-specific configuration (TenantAdmin only).
    /// </summary>
    Tenant = 1,
}
