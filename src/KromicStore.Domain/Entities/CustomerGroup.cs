// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Domain.Entities;

using System;

/// <summary>
/// Represents a customer group for segmentation.
/// </summary>
public class CustomerGroup : BaseEntity
{
    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the group name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the group description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the discount percentage for this group.
    /// </summary>
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Gets or sets whether this group is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Factory method to create a new customer group.
    /// </summary>
    public static CustomerGroup Create(
        Guid tenantId,
        string name,
        string? description = null,
        decimal discountPercentage = 0)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name is required.", nameof(name));

        return new CustomerGroup
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Description = description,
            DiscountPercentage = discountPercentage,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates the group details.
    /// </summary>
    public void Update(string name, string? description = null, decimal? discountPercentage = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name is required.", nameof(name));

        Name = name;
        Description = description;
        if (discountPercentage.HasValue)
            DiscountPercentage = discountPercentage.Value;
        UpdateTimestamp();
    }

    /// <summary>
    /// Activates the group.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdateTimestamp();
    }

    /// <summary>
    /// Deactivates the group.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }
}
