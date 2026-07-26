namespace KromicStore.Infrastructure.Proxies;

/// <summary>
/// States for the circuit breaker pattern
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// Normal operation - calls are allowed
    /// </summary>
    Closed = 0,

    /// <summary>
    /// Too many failures - calls are rejected
    /// </summary>
    Open = 1,

    /// <summary>
    /// Testing if service has recovered
    /// </summary>
    HalfOpen = 2
}
