#nullable disable

using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using KromicStore.Infrastructure.Proxies;

namespace KromicStore.Tests.Unit;

/// <summary>
/// Unit tests for ServiceProxy ExecuteAsync method retry logic with exponential backoff
/// </summary>
public class ServiceProxyTests
{
    private readonly Mock<ILogger<TestServiceProxy>> _mockLogger;
    private readonly Mock<ICircuitBreaker> _mockCircuitBreaker;
    private TestServiceProxy _serviceProxy;

    public ServiceProxyTests()
    {
        _mockLogger = new Mock<ILogger<TestServiceProxy>>();
        _mockCircuitBreaker = new Mock<ICircuitBreaker>();
        _mockCircuitBreaker.Setup(cb => cb.IsOpen).Returns(false);
    }

    [Fact]
    public async Task ExecuteAsync_WithSuccessfulOperation_ReturnsResultImmediately()
    {
        // Arrange
        _serviceProxy = new TestServiceProxy(_mockLogger.Object, _mockCircuitBreaker.Object);
        var expectedResult = "success";
        Func<Task<string>> operation = async () => await Task.FromResult(expectedResult);

        // Act
        var result = await _serviceProxy.ExecuteAsync(operation);

        // Assert
        Assert.Equal(expectedResult, result);
        _mockCircuitBreaker.Verify(cb => cb.RecordSuccess(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithCircuitBreakerOpen_ThrowsProxyException()
    {
        // Arrange
        _mockCircuitBreaker.Setup(cb => cb.IsOpen).Returns(true);
        _serviceProxy = new TestServiceProxy(_mockLogger.Object, _mockCircuitBreaker.Object);
        Func<Task<string>> operation = async () => await Task.FromResult("should not execute");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ProxyException>(() => _serviceProxy.ExecuteAsync(operation));
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task ExecuteAsync_WithRetryableFailure_RetriesWithExponentialBackoff()
    {
        // Arrange
        _serviceProxy = new TestServiceProxy(_mockLogger.Object, _mockCircuitBreaker.Object);
        int attemptCount = 0;
        var expectedResult = "success";

        Func<Task<string>> operation = async () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new HttpRequestException("Connection timeout");
            }
            return await Task.FromResult(expectedResult);
        };

        // Act
        var result = await _serviceProxy.ExecuteAsync(operation, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(3, attemptCount);
        _mockCircuitBreaker.Verify(cb => cb.RecordSuccess(), Times.Once);
        _mockCircuitBreaker.Verify(cb => cb.RecordFailure(), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_WithAllRetriesFailing_ThrowsProxyExceptionWithDetails()
    {
        // Arrange
        _serviceProxy = new TestServiceProxy(_mockLogger.Object, _mockCircuitBreaker.Object);

        Func<Task<string>> operation = async () =>
        {
            await Task.Delay(10);
            throw new HttpRequestException("Persistent connection failure");
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ProxyException>(() => _serviceProxy.ExecuteAsync(operation));
        
        // Verify the exception contains retry details
        Assert.Contains("failed after", exception.Message);
        Assert.Contains("100ms", exception.Message); // First retry delay
        Assert.Contains("1000ms", exception.Message); // Second retry delay
        Assert.Contains("10000ms", exception.Message); // Third retry delay
        Assert.Contains("30000ms", exception.Message); // Fourth retry delay
        
        // Verify circuit breaker recorded failures
        _mockCircuitBreaker.Verify(cb => cb.RecordFailure(), Times.Exactly(5)); // Initial + 4 retries
    }

    [Fact]
    public async Task ExecuteAsync_RespectsCancellationToken()
    {
        // Arrange
        _serviceProxy = new TestServiceProxy(_mockLogger.Object, _mockCircuitBreaker.Object);
        using var cts = new CancellationTokenSource();
        
        Func<Task<string>> operation = async () =>
        {
            cts.Cancel();
            await Task.Delay(1000, cts.Token); // This should throw OperationCanceledException
            return "should not reach here";
        };

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => _serviceProxy.ExecuteAsync(operation, cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeoutScenario_RetriesAfterTimeout()
    {
        // Arrange
        _serviceProxy = new TestServiceProxy(_mockLogger.Object, _mockCircuitBreaker.Object, timeoutSeconds: 1);
        int attemptCount = 0;

        Func<Task<string>> operation = async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                // First attempt times out
                await Task.Delay(2000);
            }
            return "success after timeout";
        };

        // Act
        var result = await _serviceProxy.ExecuteAsync(operation);

        // Assert
        Assert.Equal("success after timeout", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public void ExecuteAsync_WithRetryDelayArray_FollowsExponentialBackoffPattern()
    {
        // Arrange
        _serviceProxy = new TestServiceProxy(_mockLogger.Object, _mockCircuitBreaker.Object);

        // Verify the retry delays are as specified: 100ms, 1s, 10s, 30s
        var retryDelays = _serviceProxy.GetRetryDelaysMs();
        
        // Assert
        Assert.NotNull(retryDelays);
        Assert.Equal(4, retryDelays.Length);
        Assert.Equal(100, retryDelays[0]);
        Assert.Equal(1000, retryDelays[1]);
        Assert.Equal(10000, retryDelays[2]);
        Assert.Equal(30000, retryDelays[3]);
    }

    [Fact]
    public async Task ExecuteAsync_LogsRetryAttempts()
    {
        // Arrange
        _serviceProxy = new TestServiceProxy(_mockLogger.Object, _mockCircuitBreaker.Object);
        int attemptCount = 0;

        Func<Task<string>> operation = () =>
        {
            attemptCount++;
            if (attemptCount == 1)
            {
                throw new HttpRequestException("Network error");
            }
            return Task.FromResult("success");
        };

        // Act
        var result = await _serviceProxy.ExecuteAsync(operation);

        // Assert
        Assert.Equal("success", result);
        // Verify logging occurred (at least the attempt and retry logs)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Executing") || v.ToString().Contains("Retrying")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }
}

/// <summary>
/// Test implementation of ServiceProxy for unit testing
/// </summary>
public class TestServiceProxy : ServiceProxy<string>
{
    public TestServiceProxy(
        ILogger<TestServiceProxy> logger,
        ICircuitBreaker circuitBreaker,
        int timeoutSeconds = 30,
        int maxRetries = 4)
        : base(logger, circuitBreaker, timeoutSeconds, maxRetries)
    {
    }

    public int[] GetRetryDelaysMs() => RetryDelaysMs;
}
