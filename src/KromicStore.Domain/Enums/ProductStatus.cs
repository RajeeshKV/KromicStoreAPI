namespace KromicStore.Domain.Enums;

/// <summary>
/// Enumeration of product statuses in the system.
/// </summary>
public enum ProductStatus
{
    /// <summary>Product is in draft state and not yet published.</summary>
    Draft = 1,

    /// <summary>Product is active and available for purchase.</summary>
    Active = 2,

    /// <summary>Product is inactive and not available for purchase.</summary>
    Inactive = 3,

    /// <summary>Product is archived and hidden from listings.</summary>
    Archived = 4
}
