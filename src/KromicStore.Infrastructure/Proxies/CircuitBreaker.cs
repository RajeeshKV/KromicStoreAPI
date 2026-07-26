namespace KromicStore.Infrastructure.Proxies;

/// <summary>
/// Implementation of the circuit breaker pattern to prevent cascading failures
/// </summary>
public class CircuitBreaker : ICircuitBreaker
{
    private int _failureCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private readonly object _lock = new object();

    /// <summary>
    /// Threshold of consecutive failures before opening the circuit (default: 5)
    /// </summary>
    private readonly int _failureThreshold;

    /// <summary>
    /// Time in seconds to wait before attempting to transition from Open to HalfOpen (default: 30)
    /// </summary>
    private readonly int _resetTimeoutSeconds;

    /// <summary>
    /// Gets whether the circuit breaker is currently open
    /// When open, the circuit breaker rejects all calls to prevent cascading failures
    /// </summary>
    public bool IsOpen
    {
        get
        {
            lock (_lock)
            {
                // If closed or half-open, not open
                if (_state != CircuitBreakerState.Open)
                    return false;

                // If open, check if timeout has elapsed to transition to half-open
                var timeSinceLastFailure = DateTime.UtcNow - _lastFailureTime;
                if (timeSinceLastFailure >= TimeSpan.FromSeconds(_resetTimeoutSeconds))
                {
                    // Transition to half-open state to test recovery
                    _state = CircuitBreakerState.HalfOpen;
                    _failureCount = 0;
                    return false; // Half-open state allows calls through
                }

                // Still open and timeout not elapsed
                return true;
            }
        }
    }

    /// <summary>
    /// Gets the current state of the circuit breaker
    /// </summary>
    public CircuitBreakerState State
    {
        get
        {
            lock (_lock)
            {
                // Check if should transition from Open to HalfOpen
                if (_state == CircuitBreakerState.Open)
                {
                    var timeSinceLastFailure = DateTime.UtcNow - _lastFailureTime;
                    if (timeSinceLastFailure >= TimeSpan.FromSeconds(_resetTimeoutSeconds))
                    {
                        _state = CircuitBreakerState.HalfOpen;
                        _failureCount = 0;
                    }
                }
                return _state;
            }
        }
    }

    /// <summary>
    /// Gets the current failure count
    /// </summary>
    public int FailureCount
    {
        get
        {
            lock (_lock)
            {
                return _failureCount;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the CircuitBreaker class
    /// </summary>
    /// <param name="failureThreshold">Number of consecutive failures to trigger open state (default: 5)</param>
    /// <param name="resetTimeoutSeconds">Seconds to wait before attempting recovery (default: 30)</param>
    public CircuitBreaker(int failureThreshold = 5, int resetTimeoutSeconds = 30)
    {
        if (failureThreshold <= 0)
            throw new ArgumentException("Failure threshold must be greater than 0", nameof(failureThreshold));

        if (resetTimeoutSeconds <= 0)
            throw new ArgumentException("Reset timeout must be greater than 0", nameof(resetTimeoutSeconds));

        _failureThreshold = failureThreshold;
        _resetTimeoutSeconds = resetTimeoutSeconds;
    }

    /// <summary>
    /// Records a successful operation
    /// Resets failure count and closes the circuit
    /// </summary>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _state = CircuitBreakerState.Closed;
        }
    }

    /// <summary>
    /// Records a failed operation
    /// Increments failure count and opens circuit if threshold reached
    /// </summary>
    public void RecordFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_failureCount >= _failureThreshold)
            {
                _state = CircuitBreakerState.Open;
            }
        }
    }

    /// <summary>
    /// Resets the circuit breaker to closed state
    /// Used for testing or administrative purposes
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _state = CircuitBreakerState.Closed;
            _lastFailureTime = DateTime.MinValue;
        }
    }
}
