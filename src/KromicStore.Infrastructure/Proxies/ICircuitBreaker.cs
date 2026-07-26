namespace KromicStore.Infrastructure.Proxies;

/// <summary>
/// Interface for circuit breaker pattern implementation
/// </summary>
public interface ICircuitBreaker
{
    /// <summary>
    /// Gets whether the circuit breaker is currently open
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Records a successful operation, potentially closing an open circuit
    /// </summary>
    void RecordSuccess();

    /// <summary>
    /// Records a failed operation, potentially opening the circuit
    /// </summary>
    void RecordFailure();
}
