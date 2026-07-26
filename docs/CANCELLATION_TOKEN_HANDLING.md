# CancellationToken Handling in ServiceProxy

## Overview

The `ServiceProxy<TResponse>` base class implements comprehensive CancellationToken support throughout all async operations, ensuring proper handling of both caller-initiated cancellation and timeout-triggered cancellation.

## Key Principles

### 1. Caller-Initiated Cancellation (Fail Fast, No Retry)

When a caller initiates cancellation via `cancellationToken.Cancel()`:

- The operation fails immediately without retrying
- No exponential backoff delays are applied
- The `OperationCanceledException` is thrown immediately
- Circuit breaker records the failure

**Example:**
```csharp
var cts = new CancellationTokenSource();

// Start operation
var task = proxy.ExecuteAsync(() => CallExternalServiceAsync(), 
    "MyOperation", cts.Token);

// Caller decides to cancel
cts.Cancel();

// Result: OperationCanceledException thrown immediately, no retries
```

### 2. Timeout-Triggered Cancellation (Retry with Backoff)

When a timeout occurs (via internal `CancellationTokenSource.CancelAfter()`):

- Treated as a transient failure
- Retries are applied with exponential backoff
- Follows the configured retry policy: 100ms, 1s, 10s, 30s
- Circuit breaker may open after repeated timeouts

**Example:**
```csharp
// Proxy configured with 30-second timeout
var proxy = new PaymentProxy(logger, circuitBreaker);

// If external service is slow:
// Attempt 1: Times out after 30s → Retry 1 after 100ms
// Attempt 2: Times out after 30s → Retry 2 after 1s
// Attempt 3: Times out after 30s → Retry 3 after 10s
// Attempt 4: Times out after 30s → Fails, circuit breaker opens
```

### 3. Distinguishing Caller vs Timeout Cancellation

The implementation uses a linked `CancellationTokenSource` to distinguish the two scenarios:

```csharp
// Create linked token that respects BOTH:
// - Caller cancellation (cancellationToken)
// - Timeout (cts.CancelAfter)
using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

try 
{
    var result = await operation(); // Uses linked token
}
catch (OperationCanceledException ex) 
    when (cancellationToken.IsCancellationRequested && 
          ex.CancellationToken == cancellationToken)
{
    // Caller initiated cancellation → Fail fast, no retry
    throw;
}
catch (OperationCanceledException)
{
    // Timeout occurred → Retry with backoff
    // Treat as transient failure
}
```

## Implementation Details

### Cancellation Token Propagation

The `CancellationToken` is passed to ALL async operations:

1. **HttpClient calls**: `httpClient.SendAsync(request, cancellationToken)`
2. **Retry delays**: `Task.Delay(delayMs, cancellationToken)`
3. **External API calls**: All proxy methods accept `CancellationToken` parameter
4. **Middleware operations**: Propagated through the HTTP pipeline

### Retry Loop with Cancellation Support

```csharp
while (retryCount <= MaxRetries)
{
    try
    {
        // Create linked token with timeout
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));
        
        // Attempt operation
        var result = await operation();
        CircuitBreaker.RecordSuccess();
        return ProxyResult<TResponse>.Success(result);
    }
    catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
    {
        // Caller cancellation: Fail fast
        throw;
    }
    catch (OperationCanceledException)
    {
        // Timeout: Will retry below
        lastException = new TimeoutException(..., ex);
    }
    
    retryCount++;
    if (retryCount <= MaxRetries)
    {
        int delayMs = RetryDelaysMs[Math.Min(retryCount - 1, RetryDelaysMs.Length - 1)];
        
        try
        {
            // Pass cancellation token to delay - can be interrupted by caller
            await Task.Delay(delayMs, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                // Caller cancelled during delay: Propagate immediately
                throw;
            }
        }
    }
}
```

## Usage Examples

### Basic Usage with Caller Cancellation

```csharp
public async Task ProcessPaymentAsync(Order order, CancellationToken cancellationToken)
{
    var request = new CreatePaymentRequest 
    { 
        Amount = order.Total,
        Currency = "INR"
    };
    
    try 
    {
        // Pass cancellation token from controller/service
        var result = await _paymentProxy.CreatePaymentAsync(request, cancellationToken);
        
        if (result.IsSuccess)
        {
            await _orderService.ConfirmPaymentAsync(order.Id, result.Data.Id);
        }
        else
        {
            _logger.LogError("Payment failed: {Error}", result.Exception?.Message);
        }
    }
    catch (OperationCanceledException)
    {
        _logger.LogInformation("Payment processing cancelled by user");
        // Clean up partial state if needed
    }
}
```

### Middleware Cancellation Propagation

```csharp
public class TenantResolutionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        // HttpContext.RequestAborted is automatically passed to services
        var tenantId = await _tenantService.GetTenantIdAsync(
            context.User,
            context.RequestAborted); // Cancels if client disconnects
        
        context.Items["TenantId"] = tenantId;
        
        await _next(context);
    }
}
```

### Timeout Behavior

```csharp
// Configure proxy with 5-second timeout
var proxy = new PaymentProxy(logger, circuitBreaker, config);

// If external service is slow/hung:
var result = await proxy.CreatePaymentAsync(request, CancellationToken.None);

// Behavior:
// - First attempt: Waits 5s, times out
// - Retry 1: Waits 100ms before attempting, waits 5s, times out  
// - Retry 2: Waits 1s before attempting, waits 5s, times out
// - Retry 3: Waits 10s before attempting, waits 5s, times out
// - Result: ProxyResult.Failed() with timeout exception
```

## Important Considerations

### 1. Default CancellationToken Behavior

When no `CancellationToken` is provided:
```csharp
// Uses CancellationToken.None by default
var result = await proxy.ExecuteAsync(() => Operation());

// Equivalent to:
var result = await proxy.ExecuteAsync(() => Operation(), CancellationToken.None);
```

### 2. Circuit Breaker Interaction

The circuit breaker respects cancellation:
- Caller cancellation doesn't record a failure in circuit breaker (fail fast)
- Timeout cancellation DOES record failures (3+ timeouts → circuit opens)

### 3. Logging Context

All operations include cancellation information in logs:
```
Operation {OperationName} cancelled by caller | Attempt: {AttemptNumber} | 
AttemptElapsed: {ElapsedMs}ms | Service: {ServiceName}
```

### 4. No Spurious Cancellation Recovery

If cancellation occurs during a retry delay, the operation is NOT retried:
```csharp
// Caller cancels during delay
cts.Cancel();

// Does NOT continue to next retry
// Immediately throws OperationCanceledException
```

## Testing Cancellation Behavior

The test suite includes comprehensive tests:

```csharp
[Fact]
public async Task ExecuteAsync_WhenCallerInitiatesCancellation_FailsFastWithoutRetry()
{
    // Verify no retries occur
    var callCount = 0;
    var result = await proxy.ExecuteAsync(() => { callCount++; ... }, cts.Token);
    Assert.Equal(1, callCount); // Only one attempt
}

[Fact]
public async Task ExecuteAsync_WhenTimeoutOccurs_RetriesWithExponentialBackoff()
{
    // Verify retries happen with correct delays
    var result = await proxy.ExecuteAsync(() => Operation(), cts.Token);
    Assert.Equal(4, callCount); // 1 initial + 3 retries
}
```

## Graceful Cleanup

When cancellation occurs, resources are properly cleaned up:

```csharp
try 
{
    var result = await proxy.ExecuteAsync(async () =>
    {
        using var client = new HttpClient();
        return await client.SendAsync(request, cancellationToken);
    }, cancellationToken);
}
catch (OperationCanceledException)
{
    // HttpClient and other resources are disposed
    // Connection state is rolled back
}
```

## Performance Impact

CancellationToken support has minimal performance overhead:
- Linked `CancellationTokenSource` creation: < 1ms
- Token checking: O(1) operation
- Enables fast cancellation: Request can be stopped instantly

## Migration Guide

For existing code without cancellation support:

**Before:**
```csharp
var result = await paymentProxy.CreatePaymentAsync(request);
```

**After:**
```csharp
var result = await paymentProxy.CreatePaymentAsync(request, cancellationToken);
```

The default `CancellationToken.None` maintains backward compatibility while enabling new cancellation-aware code to work seamlessly.
