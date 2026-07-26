# CancellationToken Best Practices

## Guidelines for Proper CancellationToken Usage in KromicStore

### 1. Always Accept and Propagate CancellationToken

**DO:**
```csharp
public async Task<PaymentResponse> ProcessPaymentAsync(
    PaymentRequest request,
    CancellationToken cancellationToken = default)
{
    // Propagate to proxy
    var result = await _paymentProxy.CreatePaymentAsync(request, cancellationToken);
    
    // Propagate to database operations
    await _unitOfWork.Payments.AddAsync(payment, cancellationToken);
    
    return result;
}
```

**DON'T:**
```csharp
public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request)
{
    // No cancellation support - caller cannot cancel
    var result = await _paymentProxy.CreatePaymentAsync(request);
    return result;
}
```

### 2. Controller Actions Should Pass HttpContext.RequestAborted

**DO:**
```csharp
[HttpPost("payments")]
public async Task<IActionResult> CreatePayment(
    [FromBody] CreatePaymentRequest request,
    CancellationToken cancellationToken)
{
    try
    {
        var result = await _paymentService.CreatePaymentAsync(
            request, 
            cancellationToken); // ASP.NET provides this automatically
        
        return Ok(result);
    }
    catch (OperationCanceledException)
    {
        return BadRequest("Payment processing was cancelled");
    }
}
```

**DON'T:**
```csharp
[HttpPost("payments")]
public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
{
    // No cancellation token passed
    var result = await _paymentService.CreatePaymentAsync(request);
    return Ok(result);
}
```

### 3. Middleware Must Propagate Context.RequestAborted

**DO:**
```csharp
public class ServiceProxyMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context); // Passes context to downstream
        }
        catch (OperationCanceledException)
        {
            // Client disconnected - log and handle gracefully
            _logger.LogInformation("Request cancelled: {TraceId}", 
                context.TraceIdentifier);
        }
    }
}
```

**DON'T:**
```csharp
public class ServiceProxyMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        // No cancellation handling
        await _next(context);
    }
}
```

### 4. Handle OperationCanceledException Appropriately

**DO:**
```csharp
try
{
    var result = await proxy.ExecuteAsync(operation, cancellationToken);
}
catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
{
    // Caller cancelled - fail fast
    _logger.LogInformation("Operation cancelled by caller");
    throw;
}
catch (TimeoutException ex)
{
    // Timeout after retries - different handling
    _logger.LogError("Operation timed out after retries: {Message}", ex.Message);
    throw new ServiceUnavailableException("External service unavailable", ex);
}
```

**DON'T:**
```csharp
try
{
    var result = await proxy.ExecuteAsync(operation, cancellationToken);
}
catch (Exception ex)
{
    // Swallows important distinction between cancellation and timeout
    _logger.LogError("Operation failed: {Message}", ex.Message);
}
```

### 5. Use CancellationTokenSource for Compound Timeouts

**DO:**
```csharp
// Combine multiple timeout sources
using var cts = CancellationTokenSource.CreateLinkedTokenSource(
    cancellationToken,
    timeout: TimeSpan.FromSeconds(60)); // HTTP request timeout

var result = await proxy.ExecuteAsync(operation, cts.Token);
```

**DON'T:**
```csharp
// Don't create multiple independent timeouts
var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(60));

// Which timeout takes precedence? Unclear.
var result = await proxy.ExecuteAsync(operation, cts1.Token);
```

### 6. Always Dispose CancellationTokenSource

**DO:**
```csharp
using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
{
    try
    {
        var result = await proxy.ExecuteAsync(operation, cts.Token);
    }
    finally
    {
        // CTS disposed automatically
    }
}

// Or as a resource:
using var cts2 = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
var result2 = await proxy.ExecuteAsync(operation, cts2.Token);
```

**DON'T:**
```csharp
// Leaks resources
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var result = await proxy.ExecuteAsync(operation, cts.Token);
// cts never disposed
```

### 7. Retry Logic Respects Cancellation

**DO:**
```csharp
// ServiceProxy automatically retries timeouts but fails on caller cancellation
var result = await proxy.ExecuteAsync(async () =>
{
    return await httpClient.SendAsync(request, cancellationToken);
}, cancellationToken);

// If caller cancels: Fails immediately (no retries)
// If timeout occurs: Retries with exponential backoff
```

**DON'T:**
```csharp
// Don't manually retry without distinguishing cancellation types
try
{
    var result = await proxy.ExecuteAsync(operation, cancellationToken);
}
catch (OperationCanceledException)
{
    // Wrong: Don't retry on cancellation
    return await proxy.ExecuteAsync(operation, cancellationToken);
}
```

### 8. Preserve Retry Semantics on Cancellation

**DO:**
```csharp
// Let ServiceProxy handle retry decisions
var result = await _paymentProxy.CreatePaymentAsync(request, cancellationToken);

// Inspect result
if (result.IsSuccess)
{
    // Process payment
}
else if (result.RetryCount > 0)
{
    // Timeout with retries - service may be struggling
    _logger.LogWarning("Payment proxy required retries: {Count}", result.RetryCount);
}
```

**DON'T:**
```csharp
// Don't bypass retry logic
int retries = 0;
while (true)
{
    try
    {
        return await _paymentProxy.CreatePaymentAsync(request, cancellationToken);
    }
    catch (Exception)
    {
        if (++retries < 3)
            continue;
        throw;
    }
}
// ServiceProxy already handles this - don't duplicate
```

### 9. Circuit Breaker Interacts with Cancellation

**DO:**
```csharp
// Circuit breaker opens after repeated failures/timeouts
var result1 = await proxy.ExecuteAsync(operation, cancellationToken); // Timeout
var result2 = await proxy.ExecuteAsync(operation, cancellationToken); // Timeout
var result3 = await proxy.ExecuteAsync(operation, cancellationToken); // Timeout
var result4 = await proxy.ExecuteAsync(operation, cancellationToken); // Circuit open

// Cancellation doesn't affect circuit breaker
var result5 = await proxy.ExecuteAsync(operation, cancelledToken); // Throws, no CB effect
```

**DON'T:**
```csharp
// Don't assume cancellation affects circuit breaker
var cts = new CancellationTokenSource();
cts.Cancel();

var result = await proxy.ExecuteAsync(operation, cts.Token);
// This is a caller cancellation, not a failure - CB not affected
```

### 10. Log Cancellation for Debugging

**DO:**
```csharp
try
{
    var result = await proxy.ExecuteAsync(operation, cancellationToken);
}
catch (OperationCanceledException)
{
    _logger.LogInformation(
        "Operation cancelled | TraceId: {TraceId} | User: {UserId}",
        context.TraceIdentifier,
        context.User.FindFirst("sub")?.Value);
    
    // Return appropriate response
    return StatusCode(499); // "Client Closed Request"
}
```

**DON'T:**
```csharp
catch (OperationCanceledException)
{
    // Silent failure - hard to debug
    return StatusCode(500);
}
```

## Performance Best Practices

### 1. Avoid Unnecessary CancellationTokenSource Creation

**DO:**
```csharp
// Reuse token from HttpContext
public async Task<Result> ProcessAsync(CancellationToken ct)
{
    return await proxy.ExecuteAsync(operation, ct);
}

// Only create when needed for composition
using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
```

**DON'T:**
```csharp
// Unnecessary allocation per call
public async Task<Result> ProcessAsync(CancellationToken ct)
{
    using var cts = new CancellationTokenSource(); // Unused
    return await proxy.ExecuteAsync(operation, ct);
}
```

### 2. Timeout Configuration

**DO:**
```csharp
// Configure once per application
services.Configure<ProxyOptions>(options =>
{
    options.TimeoutSeconds = 30;      // Payment proxy
    options.MaxRetries = 4;            // Exponential backoff
});
```

**DON'T:**
```csharp
// Creating new timeout per request
var result = await proxy.ExecuteAsync(operation, 
    cancellationToken: new CancellationTokenSource(30000).Token); // Per-request allocation
```

## Testing Cancellation

### Test Caller Cancellation

```csharp
[Fact]
public async Task ProcessPayment_WhenCancelled_FailsFast()
{
    var cts = new CancellationTokenSource();
    var callCount = 0;

    var task = _service.ProcessPaymentAsync(request, cts.Token);
    await Task.Delay(50); // Let it start
    cts.Cancel();

    await Assert.ThrowsAsync<OperationCanceledException>(() => task);
    Assert.Equal(1, callCount); // No retries
}
```

### Test Timeout Behavior

```csharp
[Fact]
public async Task ProcessPayment_OnTimeout_RetriesWithBackoff()
{
    // Mock slow service
    var slowService = new Mock<IPaymentService>();
    slowService
        .Setup(s => s.CallAsync(It.IsAny<CancellationToken>()))
        .Returns(async (CancellationToken ct) => 
        {
            await Task.Delay(5000, ct);
            return new PaymentResponse();
        });

    var result = await _proxy.ExecuteAsync(
        () => slowService.Object.CallAsync(cts.Token),
        cancellationToken: cts.Token);

    // Should have retried
    slowService.Verify(s => s.CallAsync(It.IsAny<CancellationToken>()), 
        Times.AtLeast(2));
}
```

## Summary

| Scenario | Behavior | Retry |
|----------|----------|--------|
| Caller cancels | Fail immediately | ✗ |
| Timeout (30s) | TimeoutException | ✓ |
| HTTP 5xx | HttpRequestException | ✓ |
| Unexpected error | Re-throw | ✗ |
| Circuit breaker open | Fail immediately | ✗ |

Follow these practices to ensure proper cancellation semantics, better resource utilization, and improved user experience when operations are cancelled or time out.
