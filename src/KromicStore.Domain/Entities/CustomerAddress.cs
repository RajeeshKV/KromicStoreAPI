// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Domain.Entities;

using ValueObjects;

/// <summary>
/// Represents a customer address.
/// </summary>
public class CustomerAddress : BaseEntity
{
    /// <summary>
    /// Gets or sets the customer ID.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the address type (Billing, Shipping, Both).
    /// </summary>
    public string AddressType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the address details.
    /// </summary>
    public Address Address { get; set; } = null!;

    /// <summary>
    /// Gets or sets whether this is the default address for its type.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets the address label (e.g., "Home", "Office").
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Factory method to create a new customer address.
    /// </summary>
    public static CustomerAddress Create(
        Guid customerId,
        string addressType,
        Address address,
        bool isDefault = false,
        string? label = null)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID is required.", nameof(customerId));
        if (string.IsNullOrWhiteSpace(addressType))
            throw new ArgumentException("Address type is required.", nameof(addressType));
        if (address == null)
            throw new ArgumentNullException(nameof(address));

        return new CustomerAddress
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            AddressType = addressType,
            Address = address,
            IsDefault = isDefault,
            Label = label,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Sets this address as the default for its type.
    /// </summary>
    public void SetAsDefault()
    {
        IsDefault = true;
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the address details.
    /// </summary>
    public void UpdateAddress(Address address)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
        UpdateTimestamp();
    }
}
