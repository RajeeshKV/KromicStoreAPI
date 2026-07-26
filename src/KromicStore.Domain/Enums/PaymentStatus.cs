namespace KromicStore.Domain.Enums;

/// <summary>
/// Enumeration of payment statuses in the system.
/// </summary>
public enum PaymentStatus
{
    /// <summary>Payment is pending.</summary>
    Pending = 1,

    /// <summary>Payment has been initiated.</summary>
    Initiated = 2,

    /// <summary>Payment has been completed successfully.</summary>
    Completed = 3,

    /// <summary>Payment has failed.</summary>
    Failed = 4,

    /// <summary>Payment has been refunded.</summary>
    Refunded = 5,

    /// <summary>Payment has been cancelled.</summary>
    Cancelled = 6
}
