// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Contracts.V1.Tenants;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for tenant lifecycle operations (suspend, archive, restore, soft delete).
/// </summary>
public class TenantLifecycleRequest
{
    /// <summary>
    /// Gets or sets the reason for the lifecycle change.
    /// </summary>
    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
    public string? Reason { get; set; }
}
