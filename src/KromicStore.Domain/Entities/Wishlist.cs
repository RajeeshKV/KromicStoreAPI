// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Domain.Entities;

using System;

/// <summary>
/// Represents a customer wishlist.
/// </summary>
public class Wishlist : BaseEntity
{
    /// <summary>
    /// Gets or sets the customer ID.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the date when the item was added to wishlist.
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// Factory method to create a new wishlist item.
    /// </summary>
    public static Wishlist Create(Guid customerId, Guid productId)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID is required.", nameof(customerId));
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID is required.", nameof(productId));

        return new Wishlist
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            ProductId = productId,
            AddedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
