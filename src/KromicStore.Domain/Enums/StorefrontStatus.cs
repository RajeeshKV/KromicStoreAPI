namespace KromicStore.Domain.Enums;

/// <summary>
/// Enumeration of storefront statuses in the system.
/// </summary>
public enum StorefrontStatus
{
    /// <summary>Storefront is in draft state and not yet published.</summary>
    Draft = 0,

    /// <summary>Storefront is published and publicly accessible.</summary>
    Published = 1,

    /// <summary>Storefront is archived and no longer publicly accessible.</summary>
    Archived = 2
}
