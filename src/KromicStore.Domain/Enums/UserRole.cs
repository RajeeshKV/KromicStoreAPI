namespace KromicStore.Domain.Enums;

/// <summary>
/// Enumeration of user roles in the system.
/// </summary>
public enum UserRole
{
    /// <summary>Platform administrator with full system access.</summary>
    PlatformAdmin = 1,

    /// <summary>Tenant owner with full tenant access.</summary>
    TenantOwner = 2,

    /// <summary>Store manager with store management access.</summary>
    StoreManager = 3,

    /// <summary>Catalog editor for product management.</summary>
    CatalogEditor = 4,

    /// <summary>Customer with purchase access.</summary>
    Customer = 5,

    /// <summary>Support staff with limited access.</summary>
    Support = 6
}
