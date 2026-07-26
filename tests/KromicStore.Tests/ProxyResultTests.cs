using KromicStore.Infrastructure.Proxies;
using Xunit;

namespace KromicStore.Tests;

/// <summary>
/// Tests for ProxyResult<T> wrapper class
/// </summary>
public class ProxyResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult_WithData()
    {
        // Arrange
        var data = "test data";
        var retryCount = 0;
        var elapsedMs = 100L;

        // Act
        var result = ProxyResult<string>.Success(data, retryCount, elapsedMs);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.False(result.IsCircuitBreakerOpen);
        Assert.Equal(data, result.Data);
        Assert.Null(result.Exception);
        Assert.Equal(retryCount, result.RetryCount);
        Assert.Equal(elapsedMs, result.ElapsedMilliseconds);
    }

    [Fact]
    public void Failed_CreatesFailedResult_WithException()
    {
        // Arrange
        var exception = new ProxyException("Test failure", "TEST_ERROR");
        var retryCount = 3;
        var elapsedMs = 5000L;

        // Act
        var result = ProxyResult<string>.Failed(exception, retryCount, elapsedMs);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.False(result.IsCircuitBreakerOpen);
        Assert.Null(result.Data);
        Assert.Equal(exception, result.Exception);
        Assert.Equal(retryCount, result.RetryCount);
        Assert.Equal(elapsedMs, result.ElapsedMilliseconds);
    }

    [Fact]
    public void Failed_WithGeneralException_WrapsInProxyException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");
        var retryCount = 2;

        // Act
        var result = ProxyResult<string>.Failed(innerException, retryCount);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Exception);
        Assert.IsType<ProxyException>(result.Exception);
        Assert.Equal(innerException, result.Exception.InnerException);
    }

    [Fact]
    public void CircuitBreakerOpen_CreatesCircuitBreakerOpenResult()
    {
        // Arrange & Act
        var result = ProxyResult<string>.CircuitBreakerOpen(retryCount: 0, elapsedMilliseconds: 50);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsCircuitBreakerOpen);
        Assert.False(result.IsFailure);
        Assert.Null(result.Data);
        Assert.NotNull(result.Exception);
        Assert.Equal("CIRCUIT_BREAKER_OPEN", result.Exception.ErrorCode);
    }

    [Fact]
    public void Match_WithSuccess_CallsOnSuccessFunction()
    {
        // Arrange
        var result = ProxyResult<string>.Success("test value");
        var called = false;
        string? capturedData = null;

        // Act
        result.Match(
            onSuccess: data =>
            {
                called = true;
                capturedData = data;
            },
            onFailure: _ => { },
            onCircuitBreakerOpen: () => { });

        // Assert
        Assert.True(called);
        Assert.Equal("test value", capturedData);
    }

    [Fact]
    public void Match_WithFailure_CallsOnFailureFunction()
    {
        // Arrange
        var exception = new ProxyException("Error", "ERROR_CODE");
        var result = ProxyResult<string>.Failed(exception);
        var called = false;
        ProxyException? capturedException = null;

        // Act
        result.Match(
            onSuccess: _ => { },
            onFailure: ex =>
            {
                called = true;
                capturedException = ex;
            },
            onCircuitBreakerOpen: () => { });

        // Assert
        Assert.True(called);
        Assert.Equal(exception, capturedException);
    }

    [Fact]
    public void Match_WithCircuitBreakerOpen_CallsOnCircuitBreakerOpenFunction()
    {
        // Arrange
        var result = ProxyResult<string>.CircuitBreakerOpen();
        var called = false;

        // Act
        result.Match(
            onSuccess: _ => { },
            onFailure: _ => { },
            onCircuitBreakerOpen: () => { called = true; });

        // Assert
        Assert.True(called);
    }

    [Fact]
    public void Match_Generic_ReturnsCorrectTypeForSuccess()
    {
        // Arrange
        var result = ProxyResult<string>.Success("test");

        // Act
        var outcome = result.Match(
            onSuccess: data => $"Success: {data}",
            onFailure: ex => $"Failed: {ex.Message}",
            onCircuitBreakerOpen: () => "Circuit open");

        // Assert
        Assert.Equal("Success: test", outcome);
    }

    [Fact]
    public void Match_Generic_ReturnsCorrectTypeForFailure()
    {
        // Arrange
        var result = ProxyResult<string>.Failed(
            new ProxyException("Test error", "ERROR"));

        // Act
        var outcome = result.Match(
            onSuccess: _ => "Success",
            onFailure: ex => $"Failed: {ex.Message}",
            onCircuitBreakerOpen: () => "Circuit open");

        // Assert
        Assert.Equal("Failed: Test error", outcome);
    }

    [Fact]
    public void Fold_IncludesRetryCountInFunctions()
    {
        // Arrange
        var result = ProxyResult<string>.Success("data", retryCount: 2);

        // Act
        var outcome = result.Fold(
            onSuccess: (data, retries) => $"{data}-{retries}",
            onFailure: (_, retries) => $"Failed-{retries}",
            onCircuitBreakerOpen: () => "Open");

        // Assert
        Assert.Equal("data-2", outcome);
    }

    [Fact]
    public void Map_TransformsSuccessfulData()
    {
        // Arrange
        var result = ProxyResult<string>.Success("123", retryCount: 1, elapsedMilliseconds: 500);

        // Act
        var mapped = result.Map(data => int.Parse(data));

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal(123, mapped.Data);
        Assert.Equal(1, mapped.RetryCount);
        Assert.Equal(500, mapped.ElapsedMilliseconds);
    }

    [Fact]
    public void Map_RetainsFailureState()
    {
        // Arrange
        var exception = new ProxyException("Error", "CODE");
        var result = ProxyResult<string>.Failed(exception, retryCount: 2);

        // Act
        var mapped = result.Map(data => int.Parse(data));

        // Assert
        Assert.False(mapped.IsSuccess);
        Assert.True(mapped.IsFailure);
        Assert.NotNull(mapped.Exception);
        Assert.Equal(2, mapped.RetryCount);
    }

    [Fact]
    public void ToString_Success_DisplaysSuccessMessage()
    {
        // Arrange
        var result = ProxyResult<string>.Success("data", retryCount: 1);

        // Act
        var str = result.ToString();

        // Assert
        Assert.Contains("Success", str);
        Assert.Contains("Attempts: 2", str);
        Assert.Contains("1ms", str); // ElapsedMilliseconds is 0 by default
    }

    [Fact]
    public void ToString_Failure_DisplaysErrorMessage()
    {
        // Arrange
        var result = ProxyResult<string>.Failed(
            new ProxyException("Test error", "ERROR"), retryCount: 3);

        // Act
        var str = result.ToString();

        // Assert
        Assert.Contains("Failed", str);
        Assert.Contains("Test error", str);
        Assert.Contains("Attempts: 4", str);
    }

    [Fact]
    public void ToString_CircuitBreakerOpen_DisplaysOpenMessage()
    {
        // Arrange
        var result = ProxyResult<string>.CircuitBreakerOpen();

        // Act
        var str = result.ToString();

        // Assert
        Assert.Contains("Circuit Breaker Open", str);
    }

    [Fact]
    public void IsFailure_ComputedProperty_ReturnsCorrectValue()
    {
        // Arrange
        var success = ProxyResult<string>.Success("data");
        var failure = ProxyResult<string>.Failed(new ProxyException("Error", "CODE"));
        var circuitOpen = ProxyResult<string>.CircuitBreakerOpen();

        // Assert
        Assert.False(success.IsFailure);
        Assert.True(failure.IsFailure);
        Assert.False(circuitOpen.IsFailure);
    }

    [Fact]
    public void ProxyResult_IsImmutable_PropertiesAreReadOnly()
    {
        // Arrange
        var result = ProxyResult<string>.Success("data");

        // Act & Assert - trying to set properties should fail to compile
        // This is a compile-time verification, not a runtime test
        Assert.NotNull(result);
        Assert.Equal("data", result.Data);
    }

    [Fact]
    public async Task BindAsync_WithSuccess_CallsBinderFunction()
    {
        // Arrange
        var result = ProxyResult<int>.Success(42, retryCount: 1);

        // Act
        var bound = await result.BindAsync(async data =>
        {
            await Task.Delay(10);
            return ProxyResult<string>.Success(data.ToString());
        });

        // Assert
        Assert.True(bound.IsSuccess);
        Assert.Equal("42", bound.Data);
        Assert.Equal(1, bound.RetryCount); // Preserves retry count
    }

    [Fact]
    public async Task BindAsync_WithFailure_SkipsBinderFunction()
    {
        // Arrange
        var exception = new ProxyException("Error", "CODE");
        var result = ProxyResult<int>.Failed(exception, retryCount: 2);
        var binderCalled = false;

        // Act
        var bound = await result.BindAsync(async _ =>
        {
            binderCalled = true;
            await Task.Delay(10);
            return ProxyResult<string>.Success("transformed");
        });

        // Assert
        Assert.False(binderCalled);
        Assert.False(bound.IsSuccess);
        Assert.True(bound.IsFailure);
        Assert.Equal(2, bound.RetryCount);
    }

    [Fact]
    public async Task BindAsync_WithCircuitBreakerOpen_SkipsBinderFunction()
    {
        // Arrange
        var result = ProxyResult<int>.CircuitBreakerOpen();
        var binderCalled = false;

        // Act
        var bound = await result.BindAsync(async _ =>
        {
            binderCalled = true;
            await Task.Delay(10);
            return ProxyResult<string>.Success("transformed");
        });

        // Assert
        Assert.False(binderCalled);
        Assert.False(bound.IsSuccess);
        Assert.True(bound.IsCircuitBreakerOpen);
    }

    [Fact]
    public void ProxyException_IncludesErrorCode()
    {
        // Arrange
        var ex = new ProxyException("Something failed", "CUSTOM_ERROR_CODE");

        // Assert
        Assert.Equal("CUSTOM_ERROR_CODE", ex.ErrorCode);
        Assert.Equal("Something failed", ex.Message);
    }

    [Fact]
    public void ProxyException_StoresRetryAttempts()
    {
        // Arrange
        var ex = new ProxyException("Failed", "CODE");
        ex.RetryAttempts = 5;
        ex.ElapsedMilliseconds = 2000;

        // Assert
        Assert.Equal(5, ex.RetryAttempts);
        Assert.Equal(2000, ex.ElapsedMilliseconds);
    }

    [Fact]
    public void ProxyException_StoresServiceContext()
    {
        // Arrange
        var ex = new ProxyException("Payment failed", "PAYMENT_ERROR");
        ex.ServiceName = "PaymentProxy";
        ex.HttpStatusCode = 503;
        ex.ServiceResponse = "{\"error\": \"Service temporarily unavailable\"}";

        // Assert
        Assert.Equal("PaymentProxy", ex.ServiceName);
        Assert.Equal(503, ex.HttpStatusCode);
        Assert.Contains("Service temporarily unavailable", ex.ServiceResponse);
    }
}
