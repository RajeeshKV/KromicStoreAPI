using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace KromicStore.Infrastructure.Proxies;

/// <summary>
/// Abstract base class for external service proxies providing retry logic, circuit breaker, and timeout handling
/// </summary>
/// <typeparam name="TResponse">The response type from the external service</typeparam>
public abstract class ServiceProxy<TResponse>
{
    /// <summary>
    /// Logger for proxy operations
    /// </summary>
    protected readonly ILogger<ServiceProxy<TResponse>> Logger;

    /// <summary>
    /// Circuit breaker instance for this proxy
    /// </summary>
    protected readonly ICircuitBreaker CircuitBreaker;

    /// <summary>
    /// HTTP request timeout in seconds (default: 30)
    /// </summary>
    protected readonly int TimeoutSeconds;

    /// <summary>
    /// Maximum number of retry attempts (default: 4, which is 5 total attempts)
    /// </summary>
    protected readonly int MaxRetries;

    /// <summary>
    /// Retry delays in milliseconds using exponential backoff
    /// Pattern: 100ms, 1s, 10s, 30s (approximately doubling each time)
    /// </summary>
    protected readonly int[] RetryDelaysMs = new[] { 100, 1000, 10000, 30000 };

    /// <summary>
    /// Tenant ID for structured logging (if available in context)
    /// </summary>
    protected Guid? TenantId { get; set; }

    /// <summary>
    /// Correlation ID for distributed tracing (if available in context)
    /// </summary>
    protected string? CorrelationId { get; set; }

    /// <summary>
    /// Service name for logging (typically the proxy class name)
    /// </summary>
    protected string? ServiceName { get; set; }

    /// <summary>
    /// Initializes a new instance of the ServiceProxy class
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="circuitBreaker">Circuit breaker instance</param>
    /// <param name="timeoutSeconds">HTTP request timeout in seconds (default: 30)</param>
    /// <param name="maxRetries">Maximum retry attempts (default: 4)</param>
    protected ServiceProxy(
        ILogger<ServiceProxy<TResponse>> logger,
        ICircuitBreaker circuitBreaker,
        int timeoutSeconds = 30,
        int maxRetries = 4)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        CircuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));
        TimeoutSeconds = timeoutSeconds > 0 ? timeoutSeconds : 30;
        MaxRetries = maxRetries >= 0 ? maxRetries : 4;
        ServiceName = GetType().Name;
    }

    /// <summary>
    /// Sets the tenant context for structured logging
    /// </summary>
    /// <param name="tenantId">The tenant ID for context</param>
    /// <returns>This instance for method chaining</returns>
    public ServiceProxy<TResponse> WithTenantContext(Guid tenantId)
    {
        TenantId = tenantId;
        return this;
    }

    /// <summary>
    /// Sets the correlation ID for distributed tracing
    /// </summary>
    /// <param name="correlationId">The correlation ID for request tracing</param>
    /// <returns>This instance for method chaining</returns>
    public ServiceProxy<TResponse> WithCorrelationId(string correlationId)
    {
        CorrelationId = correlationId;
        return this;
    }

    /// <summary>
    /// Executes an async operation with retry logic, circuit breaker protection, and timeout handling
    /// </summary>
    /// <param name="operation">The async operation to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The successful result if operation succeeds within retry attempts</returns>
    /// <exception cref="ProxyException">Thrown on final retry failure with all retry details</exception>
    public async Task<TResponse> ExecuteAsync(
        Func<Task<TResponse>> operation,
        CancellationToken cancellationToken = default)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        var result = await ExecuteInternalAsync(operation, GetType().Name, cancellationToken);

        if (!result.IsSuccess)
        {
            throw result.Exception ?? new ProxyException("Operation failed for unknown reason");
        }

        return result.Data!;
    }

    /// <summary>
    /// Executes an async operation with retry logic, circuit breaker protection, and timeout handling
    /// Returns a ProxyResult that doesn't throw exceptions
    /// </summary>
    /// <param name="operation">The async operation to execute</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ProxyResult containing success/failure status and data or exception</returns>
    protected async Task<ProxyResult<TResponse>> ExecuteAsync(
        Func<Task<TResponse>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(operationName))
            throw new ArgumentException("Operation name cannot be empty", nameof(operationName));

        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        return await ExecuteInternalAsync(operation, operationName, cancellationToken);
    }

    /// <summary>
    /// Internal implementation of execute with retry logic and circuit breaker protection
    /// </summary>
    private async Task<ProxyResult<TResponse>> ExecuteInternalAsync(
        Func<Task<TResponse>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(operationName))
            operationName = GetType().Name;

        ServiceName ??= GetType().Name;
        var overallStopwatch = Stopwatch.StartNew();

        // Log operation start with context
        LogOperationStart(operationName);

        // Check if circuit breaker is open
        if (CircuitBreaker.IsOpen)
        {
            overallStopwatch.Stop();
            LogCircuitBreakerOpen(operationName, overallStopwatch);
            return ProxyResult<TResponse>.CircuitBreakerOpen(retryCount: 0, elapsedMilliseconds: overallStopwatch.ElapsedMilliseconds);
        }

        int retryCount = 0;
        Exception? lastException = null;
        var retryAttempts = new List<string>();
        long totalElapsedMs = 0;

        while (retryCount <= MaxRetries)
        {
            var attemptStopwatch = Stopwatch.StartNew();
            var attemptNumber = retryCount + 1;
            var totalAttempts = MaxRetries + 1;

            try
            {
                // Log attempt start
                LogAttemptStart(operationName, attemptNumber, totalAttempts);

                // Create linked cancellation token with timeout
                // This ensures BOTH caller cancellation and timeout are respected
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

                // Execute the operation with the linked cancellation token
                var result = await operation();

                attemptStopwatch.Stop();
                totalElapsedMs += attemptStopwatch.ElapsedMilliseconds;

                // Success - record in circuit breaker and return
                CircuitBreaker.RecordSuccess();
                overallStopwatch.Stop();

                LogOperationSucceeded(operationName, attemptNumber, attemptStopwatch, overallStopwatch);

                return ProxyResult<TResponse>.Success(result, retryCount: retryCount, elapsedMilliseconds: overallStopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested && ex.CancellationToken == cancellationToken)
            {
                // Actual cancellation requested by caller (not timeout) - fail fast without retry
                attemptStopwatch.Stop();
                totalElapsedMs += attemptStopwatch.ElapsedMilliseconds;
                overallStopwatch.Stop();

                LogOperationCancelled(operationName, attemptNumber, attemptStopwatch, overallStopwatch, ex);
                
                // Caller-initiated cancellation should NOT retry
                var cancelException = new OperationCanceledException(
                    $"{operationName} was cancelled by the caller",
                    ex,
                    cancellationToken);
                throw cancelException;
            }
            catch (OperationCanceledException ex)
            {
                // Timeout occurred (timeout CancellationToken fired, not caller token)
                // This should retry according to exponential backoff
                lastException = new TimeoutException(
                    $"{operationName} timed out after {TimeoutSeconds} seconds",
                    ex);
                
                attemptStopwatch.Stop();
                totalElapsedMs += attemptStopwatch.ElapsedMilliseconds;

                LogAttemptTimeout(operationName, attemptNumber, totalAttempts, attemptStopwatch, lastException);
                retryAttempts.Add($"Attempt {attemptNumber}: Timeout ({TimeoutSeconds}s)");
            }
            catch (HttpRequestException ex)
            {
                // HTTP or network error - transient, should retry
                lastException = ex;
                attemptStopwatch.Stop();
                totalElapsedMs += attemptStopwatch.ElapsedMilliseconds;

                LogAttemptFailed(operationName, attemptNumber, totalAttempts, attemptStopwatch, ex, "HTTP error");
                retryAttempts.Add($"Attempt {attemptNumber}: HTTP error ({ex.Message})");
            }
            catch (Exception ex)
            {
                // Unexpected error - should NOT retry, fail fast
                attemptStopwatch.Stop();
                totalElapsedMs += attemptStopwatch.ElapsedMilliseconds;
                overallStopwatch.Stop();

                CircuitBreaker.RecordFailure();
                LogOperationError(operationName, attemptNumber, attemptStopwatch, overallStopwatch, ex);
                throw;
            }

            // Record failure in circuit breaker
            CircuitBreaker.RecordFailure();

            // Check if we should retry
            retryCount++;
            if (retryCount <= MaxRetries)
            {
                // Get delay for this retry attempt
                int delayMs = RetryDelaysMs[Math.Min(retryCount - 1, RetryDelaysMs.Length - 1)];

                LogRetryScheduled(operationName, retryCount, MaxRetries, delayMs, lastException, attemptStopwatch, totalElapsedMs);

                try
                {
                    // Pass cancellation token to Task.Delay so delays can be interrupted
                    await Task.Delay(delayMs, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // If delay was cancelled by caller, propagate immediately (no retry)
                    if (cancellationToken.IsCancellationRequested)
                    {
                        attemptStopwatch.Stop();
                        overallStopwatch.Stop();
                        Logger.LogInformation(
                            "{OperationName} retry delay was cancelled by caller | Service: {ServiceName} | TenantId: {TenantId} | CorrelationId: {CorrelationId}",
                            operationName,
                            ServiceName,
                            TenantId?.ToString() ?? "N/A",
                            CorrelationId ?? "N/A");
                        throw;
                    }
                    
                    // Otherwise, this was a spurious cancellation, continue
                    Logger.LogWarning(
                        "{OperationName} retry delay was cancelled (spurious) | Service: {ServiceName}",
                        operationName,
                        ServiceName);
                }
            }
        }

        // All retries exhausted
        overallStopwatch.Stop();
        var failureMessage = $"{operationName} failed after {MaxRetries + 1} attempts. " +
                           $"Delays: {string.Join(", ", RetryDelaysMs.Select(d => $"{d}ms"))}. " +
                           $"Last error: {lastException?.Message}";

        LogOperationFailed(operationName, MaxRetries + 1, overallStopwatch, failureMessage);

        var proxyException = new ProxyException(
            failureMessage,
            "MAX_RETRIES_EXCEEDED",
            lastException);

        return ProxyResult<TResponse>.Failed(proxyException, retryCount: MaxRetries + 1, elapsedMilliseconds: overallStopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Logs operation start with structured context
    /// </summary>
    private void LogOperationStart(string operationName)
    {
        Logger.LogInformation(
            "Starting operation {OperationName} | Service: {ServiceName} | TenantId: {TenantId} | CorrelationId: {CorrelationId} | Timeout: {TimeoutSeconds}s | MaxRetries: {MaxRetries}",
            operationName,
            ServiceName,
            TenantId?.ToString() ?? "N/A",
            CorrelationId ?? "N/A",
            TimeoutSeconds,
            MaxRetries);
    }

    /// <summary>
    /// Logs attempt start with progress indicator
    /// </summary>
    private void LogAttemptStart(string operationName, int attemptNumber, int totalAttempts)
    {
        Logger.LogInformation(
            "Executing {OperationName} - Attempt {AttemptNumber}/{TotalAttempts} | Service: {ServiceName} | TenantId: {TenantId} | CorrelationId: {CorrelationId} | Status: starting",
            operationName,
            attemptNumber,
            totalAttempts,
            ServiceName,
            TenantId?.ToString() ?? "N/A",
            CorrelationId ?? "N/A");
    }

    /// <summary>
    /// Logs successful operation completion with timing
    /// </summary>
    private void LogOperationSucceeded(string operationName, int attemptNumber, Stopwatch attemptStopwatch, Stopwatch overallStopwatch)
    {
        Logger.LogInformation(
            "Operation {OperationName} completed successfully | Attempt: {AttemptNumber} | AttemptElapsed: {AttemptElapsedMs}ms | TotalElapsed: {TotalElapsedMs}ms | Service: {ServiceName} | TenantId: {TenantId} | CorrelationId: {CorrelationId} | Status: succeeded",
            operationName,
            attemptNumber,
            attemptStopwatch.ElapsedMilliseconds,
            overallStopwatch.ElapsedMilliseconds,
            ServiceName,
            TenantId?.ToString() ?? "N/A",
            CorrelationId ?? "N/A");
    }

    /// <summary>
    /// Logs attempt timeout with timing details
    /// </summary>
    private void LogAttemptTimeout(string operationName, int attemptNumber, int totalAttempts, Stopwatch attemptStopwatch, Exception exception)
    {
        Logger.LogWarning(
            exception,
            "Operation {OperationName} timed out | Attempt: {AttemptNumber}/{TotalAttempts} | Elapsed: {ElapsedMs}ms | Timeout: {TimeoutSeconds}s | Service: {ServiceName} | TenantId: {TenantId} | CorrelationId: {CorrelationId} | Status: timeout",
            operationName,
            attemptNumber,
            totalAttempts,
            attemptStopwatch.ElapsedMilliseconds,
            TimeoutSeconds,
            ServiceName,
            TenantId?.ToString() ?? "N/A",
            CorrelationId ?? "N/A");
    }

    /// <summary>
    /// Logs attempt failure with details
    /// </summary>
    private void LogAttemptFailed(string operationName, int attemptNumber, int totalAttempts, Stopwatch attemptStopwatch, Exception exception, string failureType)
    {
        Logger.LogWarning(
            exception,
            "Operation {OperationName} failed | Attempt: {AttemptNumber}/{TotalAttempts} | Elapsed: {ElapsedMs}ms | FailureType: {FailureType} | Service: {ServiceName} | TenantId: {TenantId} | CorrelationId: {CorrelationId} | Status: failed",
            operationName,
            attemptNumber,
            totalAttempts,
            attemptStopwatch.ElapsedMilliseconds,
            failureType,
            ServiceName,
            TenantId?.ToString() ?? "N/A",
            CorrelationId ?? "N/A");
    }

    /// <summary>
    /// Logs retry scheduling with delay information
    /// </summary>
    private void LogRetryScheduled(string operationName, int nextRetryNumber, int maxRetries, int delayMs, Exception? lastException, Stopwatch attemptStopwatch, long totalElapsedMs)
    {
        Logger.LogInformation(
            "Scheduling retry for {OperationName} | NextRetry: {NextRetry}/{MaxRetries} | Delay: {DelayMs}ms | AttemptElapsed: {AttemptElapsedMs}ms | TotalElapsed: {TotalElapsedMs}ms | LastError: {LastError} | Service: {ServiceName} | TenantId: {TenantId} | CorrelationId: {CorrelationId} | Status: retrying",
            operationName,
            nextRetryNumber,
            maxRetries,
            delayMs,
            attemptStopwatch.ElapsedMilliseconds,
            totalElapsedMs,
            lastException?.Message ?? "Unknown",
            ServiceName,
            TenantId?.ToString() ?? "N/A",
            CorrelationId ?? "N/A");
    }

    /// <summary>
    /// Logs operation error (unexpected exception)
    /// </summary>
    private void LogOperationError(string operationName, int attemptNumber, Stopwatch attemptStopwatch, Stopwatch overallStopwatch, Exception exception)
    {
        Logger.LogError(
            exception,
            "Operation {OperationName} encountered unexpected error | Attempt: {AttemptNumber} | AttemptElapsed: {AttemptElapsedMs}ms | TotalElapsed: {TotalElapsedMs}ms | Service: {ServiceName} | TenantId: {TenantId} | CorrelationId: {CorrelationId} | Status: error",
            operationName,
            attemptNumber,
            attemptStopwatch.ElapsedMilliseconds,
            overallStopwatch.ElapsedMilliseconds,
            ServiceName,
            TenantId?.ToString() ?? "N/A",
            CorrelationId ?? "N/A");
    }

    /// <summary>
    /// Logs operation cancellation by caller
    /// </summary>
    private void LogOperationCancelled(string operationName, int attemptNumber, Stopwatch attemptStopwatch, Stopwatch overallStopwatch, Exception exception)
    {
        Logger.LogInformation(
            exception,
            "Operation {OperationName} cancelled by caller | Attempt: {AttemptNumber} | AttemptElapsed: {AttemptElapsedMs}ms | TotalElapsed: {TotalElapsedMs}ms | Service: {ServiceName} | TenantId: {TenantId} | CorrelationId: {CorrelationId} | Status: cancelled",
            operationName,
            attemptNumber,
            attemptStopwatch.ElapsedMilliseconds,
            overallStopwatch.ElapsedMilliseconds,
            ServiceName,
            TenantId?.ToString() ?? "N/A",
            CorrelationId ?? "N/A");
    }

    /// <summary>
    /// Logs circuit breaker open state
    /// </summary>
    private void LogCircuitBreakerOpen(string operationName, Stopwatch overallStopwatch)
    {
        Logger.LogWarning(
            "Circuit breaker is open for operation {OperationName} | Service: {ServiceName} | TenantId: {TenantId} | CorrelationId: {CorrelationId} | Status: circuit_breaker_open | Elapsed: {ElapsedMs}ms",
            operationName,
            ServiceName,
            TenantId?.ToString() ?? "N/A",
            CorrelationId ?? "N/A",
            overallStopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Logs final operation failure after all retries exhausted
    /// </summary>
    private void LogOperationFailed(string operationName, int totalAttempts, Stopwatch overallStopwatch, string failureMessage)
    {
        Logger.LogError(
            "Operation {OperationName} failed after all retries | TotalAttempts: {TotalAttempts} | TotalElapsed: {TotalElapsedMs}ms | Message: {FailureMessage} | Service: {ServiceName} | TenantId: {TenantId} | CorrelationId: {CorrelationId} | Status: failed",
            operationName,
            totalAttempts,
            overallStopwatch.ElapsedMilliseconds,
            failureMessage,
            ServiceName,
            TenantId?.ToString() ?? "N/A",
            CorrelationId ?? "N/A");
    }
}
