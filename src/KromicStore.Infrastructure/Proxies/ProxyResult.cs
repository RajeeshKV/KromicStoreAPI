using System.Text.Json.Serialization;

namespace KromicStore.Infrastructure.Proxies;

/// <summary>
/// Result wrapper for proxy operations handling success, failure, and circuit breaker states.
/// This immutable class represents the outcome of an external service call with retry tracking and timing information.
/// </summary>
/// <remarks>
/// The ProxyResult<T> provides a type-safe, non-throwing way to handle proxy operation outcomes.
/// It tracks retry attempts, execution timing, and categorizes failures into three states:
/// - Success: Operation completed successfully with data
/// - Failure: Operation failed after exhausting retries
/// - CircuitBreakerOpen: Circuit breaker prevented the call
/// </remarks>
/// <typeparam name="T">The type of data returned on success</typeparam>
[Serializable]
public class ProxyResult<T>
{
    /// <summary>
    /// Indicates whether the operation succeeded
    /// </summary>
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; }

    /// <summary>
    /// Indicates whether the circuit breaker is open
    /// </summary>
    [JsonPropertyName("isCircuitBreakerOpen")]
    public bool IsCircuitBreakerOpen { get; }

    /// <summary>
    /// Indicates whether the operation failed (not successful and no circuit breaker)
    /// </summary>
    [JsonIgnore]
    public bool IsFailure => !IsSuccess && !IsCircuitBreakerOpen;

    /// <summary>
    /// The result data (only valid when IsSuccess is true)
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; }

    /// <summary>
    /// The exception that occurred (only populated if IsSuccess is false)
    /// </summary>
    [JsonPropertyName("exception")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProxyException? Exception { get; }

    /// <summary>
    /// Number of retry attempts made before final outcome
    /// </summary>
    [JsonPropertyName("retryCount")]
    public int RetryCount { get; }

    /// <summary>
    /// Total elapsed time in milliseconds for the operation (including all retries)
    /// </summary>
    [JsonPropertyName("elapsedMilliseconds")]
    public long ElapsedMilliseconds { get; }

    /// <summary>
    /// Timestamp when the operation was initiated
    /// </summary>
    [JsonPropertyName("initiatedAt")]
    public DateTime InitiatedAt { get; }

    /// <summary>
    /// Timestamp when the operation completed
    /// </summary>
    [JsonPropertyName("completedAt")]
    public DateTime CompletedAt { get; }

    /// <summary>
    /// Private constructor to ensure immutability
    /// </summary>
    private ProxyResult(
        bool isSuccess,
        bool isCircuitBreakerOpen,
        T? data,
        ProxyException? exception,
        int retryCount,
        long elapsedMilliseconds)
    {
        IsSuccess = isSuccess;
        IsCircuitBreakerOpen = isCircuitBreakerOpen;
        Data = data;
        Exception = exception;
        RetryCount = retryCount;
        ElapsedMilliseconds = elapsedMilliseconds;
        InitiatedAt = DateTime.UtcNow;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a successful result with data and retry information
    /// </summary>
    /// <param name="data">The result data</param>
    /// <param name="retryCount">Number of retries before success (default: 0)</param>
    /// <param name="elapsedMilliseconds">Total elapsed time in milliseconds</param>
    /// <returns>A successful ProxyResult<T></returns>
    public static ProxyResult<T> Success(T data, int retryCount = 0, long elapsedMilliseconds = 0) => new(
        isSuccess: true,
        isCircuitBreakerOpen: false,
        data: data,
        exception: null,
        retryCount: retryCount,
        elapsedMilliseconds: elapsedMilliseconds);

    /// <summary>
    /// Creates a failed result with exception and retry information
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="retryCount">Number of retries attempted before failure</param>
    /// <param name="elapsedMilliseconds">Total elapsed time in milliseconds</param>
    /// <returns>A failed ProxyResult<T></returns>
    public static ProxyResult<T> Failed(
        ProxyException exception,
        int retryCount = 0,
        long elapsedMilliseconds = 0) => new(
        isSuccess: false,
        isCircuitBreakerOpen: false,
        data: default,
        exception: exception ?? throw new ArgumentNullException(nameof(exception)),
        retryCount: retryCount,
        elapsedMilliseconds: elapsedMilliseconds);

    /// <summary>
    /// Creates a failed result from a general exception (converts to ProxyException)
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="retryCount">Number of retries attempted before failure</param>
    /// <param name="elapsedMilliseconds">Total elapsed time in milliseconds</param>
    /// <returns>A failed ProxyResult<T></returns>
    public static ProxyResult<T> Failed(
        Exception exception,
        int retryCount = 0,
        long elapsedMilliseconds = 0)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        var proxyException = exception is ProxyException pe
            ? pe
            : new ProxyException(exception.Message, "UNKNOWN", exception);

        return Failed(proxyException, retryCount, elapsedMilliseconds);
    }

    /// <summary>
    /// Creates a circuit breaker open result
    /// </summary>
    /// <param name="retryCount">Number of retries in the current attempt (typically 0)</param>
    /// <param name="elapsedMilliseconds">Elapsed time before circuit breaker rejection</param>
    /// <returns>A circuit breaker open ProxyResult<T></returns>
    public static ProxyResult<T> CircuitBreakerOpen(int retryCount = 0, long elapsedMilliseconds = 0) => new(
        isSuccess: false,
        isCircuitBreakerOpen: true,
        data: default,
        exception: new ProxyException("Circuit breaker is open", errorCode: "CIRCUIT_BREAKER_OPEN"),
        retryCount: retryCount,
        elapsedMilliseconds: elapsedMilliseconds);

    /// <summary>
    /// Pattern matches on the result state and applies appropriate function
    /// </summary>
    /// <remarks>
    /// This method enables functional programming patterns for handling different result states.
    /// Example:
    /// <code>
    /// var result = await proxy.ExecuteAsync(...);
    /// var outcome = result.Match(
    ///     onSuccess: data => $"Success: {data}",
    ///     onFailure: ex => $"Failed: {ex.Message}",
    ///     onCircuitBreakerOpen: () => "Service unavailable - circuit open");
    /// </code>
    /// </remarks>
    /// <param name="onSuccess">Function to apply when operation succeeds</param>
    /// <param name="onFailure">Function to apply when operation fails</param>
    /// <param name="onCircuitBreakerOpen">Function to apply when circuit breaker is open</param>
    /// <returns>The result of the applied function</returns>
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<ProxyException, TResult> onFailure,
        Func<TResult> onCircuitBreakerOpen)
    {
        if (onSuccess == null) throw new ArgumentNullException(nameof(onSuccess));
        if (onFailure == null) throw new ArgumentNullException(nameof(onFailure));
        if (onCircuitBreakerOpen == null) throw new ArgumentNullException(nameof(onCircuitBreakerOpen));

        return IsSuccess ? onSuccess(Data!)
            : IsCircuitBreakerOpen ? onCircuitBreakerOpen()
            : onFailure(Exception!);
    }

    /// <summary>
    /// Pattern matches on the result state and applies appropriate action
    /// </summary>
    /// <remarks>
    /// This is the void-returning variant of Match, useful for side effects.
    /// Example:
    /// <code>
    /// result.Match(
    ///     onSuccess: data => Console.WriteLine($"Success: {data}"),
    ///     onFailure: ex => logger.LogError(ex, "Operation failed"),
    ///     onCircuitBreakerOpen: () => alerting.NotifyOutage());
    /// </code>
    /// </remarks>
    /// <param name="onSuccess">Action to execute when operation succeeds</param>
    /// <param name="onFailure">Action to execute when operation fails</param>
    /// <param name="onCircuitBreakerOpen">Action to execute when circuit breaker is open</param>
    public void Match(
        Action<T> onSuccess,
        Action<ProxyException> onFailure,
        Action onCircuitBreakerOpen)
    {
        if (onSuccess == null) throw new ArgumentNullException(nameof(onSuccess));
        if (onFailure == null) throw new ArgumentNullException(nameof(onFailure));
        if (onCircuitBreakerOpen == null) throw new ArgumentNullException(nameof(onCircuitBreakerOpen));

        if (IsSuccess)
            onSuccess(Data!);
        else if (IsCircuitBreakerOpen)
            onCircuitBreakerOpen();
        else
            onFailure(Exception!);
    }

    /// <summary>
    /// Folds the result into a single value using provided functions
    /// </summary>
    /// <remarks>
    /// Fold is similar to Match but often used for transforming results in functional chains.
    /// </remarks>
    /// <typeparam name="TResult">The type of the result</typeparam>
    /// <param name="onSuccess">Function to apply on success, receives data and retry count</param>
    /// <param name="onFailure">Function to apply on failure, receives exception and retry count</param>
    /// <param name="onCircuitBreakerOpen">Function to apply when circuit breaker is open</param>
    /// <returns>The folded result</returns>
    public TResult Fold<TResult>(
        Func<T, int, TResult> onSuccess,
        Func<ProxyException, int, TResult> onFailure,
        Func<TResult> onCircuitBreakerOpen)
    {
        if (onSuccess == null) throw new ArgumentNullException(nameof(onSuccess));
        if (onFailure == null) throw new ArgumentNullException(nameof(onFailure));
        if (onCircuitBreakerOpen == null) throw new ArgumentNullException(nameof(onCircuitBreakerOpen));

        return IsSuccess ? onSuccess(Data!, RetryCount)
            : IsCircuitBreakerOpen ? onCircuitBreakerOpen()
            : onFailure(Exception!, RetryCount);
    }

    /// <summary>
    /// Transforms the data in a successful result, or returns the current failed/open result
    /// </summary>
    /// <remarks>
    /// This enables functional chaining without pattern matching.
    /// Example:
    /// <code>
    /// var result = await proxy.ExecuteAsync(...)
    ///     .Map(data => new ProcessedData { Value = data.ProcessedValue });
    /// </code>
    /// </remarks>
    /// <typeparam name="TNew">The new result type</typeparam>
    /// <param name="mapper">Function to transform successful data</param>
    /// <returns>A new ProxyResult with transformed data, or the current failed/open result</returns>
    public ProxyResult<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        if (mapper == null) throw new ArgumentNullException(nameof(mapper));

        if (IsSuccess)
        {
            var transformedData = mapper(Data!);
            return ProxyResult<TNew>.Success(transformedData, RetryCount, ElapsedMilliseconds);
        }

        if (IsCircuitBreakerOpen)
            return ProxyResult<TNew>.CircuitBreakerOpen(RetryCount, ElapsedMilliseconds);

        return ProxyResult<TNew>.Failed(Exception!, RetryCount, ElapsedMilliseconds);
    }

    /// <summary>
    /// Chains multiple proxy operations together
    /// </summary>
    /// <remarks>
    /// Bind (also called FlatMap) allows composing multiple proxy operations.
    /// If the current result is a failure or circuit breaker open, the binder is not called.
    /// Example:
    /// <code>
    /// var result = await proxy1.ExecuteAsync(...)
    ///     .Bind(data => proxy2.ExecuteAsync(..., data));
    /// </code>
    /// </remarks>
    /// <typeparam name="TNew">The new result type</typeparam>
    /// <param name="binder">Function that takes successful data and returns a new ProxyResult</param>
    /// <returns>The result of the binder, or the current failed/open result</returns>
    public async Task<ProxyResult<TNew>> BindAsync<TNew>(Func<T, Task<ProxyResult<TNew>>> binder)
    {
        if (binder == null) throw new ArgumentNullException(nameof(binder));

        if (IsSuccess)
            return await binder(Data!);

        if (IsCircuitBreakerOpen)
            return ProxyResult<TNew>.CircuitBreakerOpen(RetryCount, ElapsedMilliseconds);

        return ProxyResult<TNew>.Failed(Exception!, RetryCount, ElapsedMilliseconds);
    }

    /// <summary>
    /// Gets a summary string representation of the result
    /// </summary>
    /// <remarks>
    /// Useful for logging and debugging.
    /// </remarks>
    /// <returns>A formatted summary of the result state</returns>
    public override string ToString()
    {
        if (IsSuccess)
            return $"Success (Attempts: {RetryCount + 1}, Elapsed: {ElapsedMilliseconds}ms)";

        if (IsCircuitBreakerOpen)
            return $"Circuit Breaker Open (Attempts: {RetryCount + 1}, Elapsed: {ElapsedMilliseconds}ms)";

        return $"Failed: {Exception?.Message} (Attempts: {RetryCount + 1}, Elapsed: {ElapsedMilliseconds}ms)";
    }
}
