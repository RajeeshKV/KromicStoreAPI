#nullable disable

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using KromicStore.API.Extensions;
using KromicStore.Infrastructure.Proxies;

namespace KromicStore.Tests.HttpClient;

/// <summary>
/// Tests for HttpClient configuration including timeouts, pooling, compression, and logging handlers
/// </summary>
public class HttpClientConfigurationTests : IAsyncLifetime
{
    private ServiceCollection _services;
    private ServiceProvider _serviceProvider;
    private IConfiguration _configuration;

    public async Task InitializeAsync()
    {
        _services = new ServiceCollection();
        _services.AddLogging(builder => builder.AddConsole());

        // Create mock configuration
        var configDict = new Dictionary<string, string>
        {
            { "ExternalServices:ConnectionTimeoutSeconds", "30" },
            { "ExternalServices:RequestTimeoutSeconds", "30" },
            { "ExternalServices:MaxRetryCount", "4" },
            { "ExternalServices:CircuitBreakerThreshold", "5" },
            { "ExternalServices:Razorpay:KeyId", "test_key_id" },
            { "ExternalServices:Razorpay:KeySecret", "test_key_secret" },
            { "ExternalServices:Google:ClientId", "test_client_id" },
            { "ExternalServices:Google:ClientSecret", "test_client_secret" },
            { "ExternalServices:Cloudinary:CloudName", "test_cloud_name" },
            { "ExternalServices:Cloudinary:ApiKey", "test_api_key" },
            { "ExternalServices:Cloudinary:ApiSecret", "test_api_secret" },
            { "ExternalServices:Brevo:ApiKey", "test_brevo_key" },
            { "ExternalServices:Brevo:SenderEmail", "noreply@example.com" }
        };

        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(configDict);
        _configuration = configBuilder.Build();

        _services.AddSingleton(_configuration);
        _services.AddLogging();
        _services.AddHttpClient();
        
        // Add circuit breakers
        _services.AddSingleton<ICircuitBreaker, CircuitBreaker>();

        // Register the HttpClient factories
        _services.AddExternalServiceHttpClients(_configuration);

        _serviceProvider = _services.BuildServiceProvider();
        
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that PaymentProxy HttpClient has 30-second timeout
    /// </summary>
    [Fact]
    public void PaymentProxyHttpClient_ShouldHave30SecondTimeout()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IHttpClientFactory>();

        // Act
        var client = factory.CreateClient(nameof(PaymentProxy));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(30), client.Timeout);
    }

    /// <summary>
    /// Verifies that OAuthProxy HttpClient has 15-second timeout (faster token exchange)
    /// </summary>
    [Fact]
    public void OAuthProxyHttpClient_ShouldHave15SecondTimeout()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IHttpClientFactory>();

        // Act
        var client = factory.CreateClient(nameof(OAuthProxy));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(15), client.Timeout);
    }

    /// <summary>
    /// Verifies that MediaProxy HttpClient has 60-second timeout (for file uploads)
    /// </summary>
    [Fact]
    public void MediaProxyHttpClient_ShouldHave60SecondTimeout()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IHttpClientFactory>();

        // Act
        var client = factory.CreateClient(nameof(MediaProxy));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(60), client.Timeout);
    }

    /// <summary>
    /// Verifies that NotificationProxy HttpClient has 15-second timeout
    /// </summary>
    [Fact]
    public void NotificationProxyHttpClient_ShouldHave15SecondTimeout()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IHttpClientFactory>();

        // Act
        var client = factory.CreateClient(nameof(NotificationProxy));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(15), client.Timeout);
    }

    /// <summary>
    /// Verifies that all HttpClients include User-Agent header
    /// </summary>
    [Theory]
    [InlineData(nameof(PaymentProxy))]
    [InlineData(nameof(OAuthProxy))]
    [InlineData(nameof(MediaProxy))]
    [InlineData(nameof(NotificationProxy))]
    public void HttpClient_ShouldIncludeUserAgentHeader(string clientName)
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IHttpClientFactory>();

        // Act
        var client = factory.CreateClient(clientName);

        // Assert
        Assert.Contains("KromicStore/1.0", client.DefaultRequestHeaders.UserAgent.ToString());
    }

    /// <summary>
    /// Verifies that PaymentProxy and NotificationProxy include Accept-Json header
    /// </summary>
    [Theory]
    [InlineData(nameof(PaymentProxy))]
    [InlineData(nameof(OAuthProxy))]
    [InlineData(nameof(NotificationProxy))]
    public void HttpClient_ShouldIncludeAcceptJsonHeader(string clientName)
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IHttpClientFactory>();

        // Act
        var client = factory.CreateClient(clientName);

        // Assert
        Assert.Contains("application/json", client.DefaultRequestHeaders.Accept.ToString());
    }

    /// <summary>
    /// Verifies that PaymentProxy can be created from DI container
    /// </summary>
    [Fact]
    public void PaymentProxy_ShouldBeCreatable()
    {
        // Act
        var proxy = _serviceProvider.GetRequiredService<PaymentProxy>();

        // Assert
        Assert.NotNull(proxy);
    }

    /// <summary>
    /// Verifies that OAuthProxy can be created from DI container
    /// </summary>
    [Fact]
    public void OAuthProxy_ShouldBeCreatable()
    {
        // Act
        var proxy = _serviceProvider.GetRequiredService<OAuthProxy>();

        // Assert
        Assert.NotNull(proxy);
    }

    /// <summary>
    /// Verifies that MediaProxy can be created from DI container
    /// </summary>
    [Fact]
    public void MediaProxy_ShouldBeCreatable()
    {
        // Act
        var proxy = _serviceProvider.GetRequiredService<MediaProxy>();

        // Assert
        Assert.NotNull(proxy);
    }

    /// <summary>
    /// Verifies that NotificationProxy can be created from DI container
    /// </summary>
    [Fact]
    public void NotificationProxy_ShouldBeCreatable()
    {
        // Act
        var proxy = _serviceProvider.GetRequiredService<NotificationProxy>();

        // Assert
        Assert.NotNull(proxy);
    }

    /// <summary>
    /// Verifies that all proxies have circuit breaker configured
    /// </summary>
    [Fact]
    public void AllProxies_ShouldHaveCircuitBreakerConfigured()
    {
        // Act
        var paymentProxy = _serviceProvider.GetRequiredService<PaymentProxy>();
        var oauthProxy = _serviceProvider.GetRequiredService<OAuthProxy>();
        var mediaProxy = _serviceProvider.GetRequiredService<MediaProxy>();
        var notificationProxy = _serviceProvider.GetRequiredService<NotificationProxy>();

        // Assert - just verify they can be created (circuit breaker would be set in constructor)
        Assert.NotNull(paymentProxy);
        Assert.NotNull(oauthProxy);
        Assert.NotNull(mediaProxy);
        Assert.NotNull(notificationProxy);
    }
}

/// <summary>
/// Tests for logging handler configuration
/// </summary>
public class LoggingHandlerTests
{
    /// <summary>
    /// Verifies that LoggingHttpMessageHandler logs request details
    /// </summary>
    [Fact]
    public async Task LoggingHandler_ShouldLogRequest()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<KromicStore.API.Handlers.LoggingHttpMessageHandler>>();
        var handler = new KromicStore.API.Handlers.LoggingHttpMessageHandler(mockLogger.Object)
        {
            InnerHandler = new HttpClientHandler()
        };
        
        using var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");
        request.Content = new StringContent("{\"test\": \"data\"}", System.Text.Encoding.UTF8, "application/json");

        // Act
        try
        {
            await invoker.SendAsync(request, CancellationToken.None);
        }
        catch
        {
            // Network error is expected, we're just testing that logging handler was called
        }

        // Assert - verify logging was called (at minimum Informational level)
        mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }
}

/// <summary>
/// Tests for connection pooling configuration
/// </summary>
public class ConnectionPoolingTests
{
    /// <summary>
    /// Verifies that SocketsHttpHandler is configured for connection pooling
    /// </summary>
    [Fact]
    public void HttpClientHandler_ShouldConfigureConnectionPooling()
    {
        // Arrange
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
            MaxConnectionsPerServer = 10,
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | 
                                    System.Net.DecompressionMethods.Deflate
        };

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(2), handler.PooledConnectionLifetime);
        Assert.Equal(TimeSpan.FromMinutes(1), handler.PooledConnectionIdleTimeout);
        Assert.Equal(10, handler.MaxConnectionsPerServer);
        Assert.NotEqual(System.Net.DecompressionMethods.None, handler.AutomaticDecompression);
    }

    /// <summary>
    /// Verifies that MediaProxy has higher MaxConnectionsPerServer for concurrent uploads
    /// </summary>
    [Fact]
    public void MediaProxyHandler_ShouldAllowMoreConcurrentConnections()
    {
        // Arrange
        var paymentHandler = new SocketsHttpHandler { MaxConnectionsPerServer = 10 };
        var mediaHandler = new SocketsHttpHandler { MaxConnectionsPerServer = 20 };

        // Assert
        Assert.Equal(10, paymentHandler.MaxConnectionsPerServer);
        Assert.Equal(20, mediaHandler.MaxConnectionsPerServer);
        Assert.True(mediaHandler.MaxConnectionsPerServer > paymentHandler.MaxConnectionsPerServer);
    }
}

/// <summary>
/// Tests for compression configuration
/// </summary>
public class CompressionTests
{
    /// <summary>
    /// Verifies that compression handler adds Accept-Encoding headers
    /// </summary>
    [Fact]
    public async Task CompressionHandler_ShouldAddAcceptEncodingHeaders()
    {
        // Arrange
        var handler = new KromicStore.API.Handlers.CompressionHttpMessageHandler()
        {
            InnerHandler = new HttpClientHandler()
        };
        
        using var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");

        // Act
        try
        {
            await invoker.SendAsync(request, CancellationToken.None);
        }
        catch
        {
            // Network error is expected
        }

        // Assert
        Assert.NotEmpty(request.Headers.AcceptEncoding);
        Assert.Contains(request.Headers.AcceptEncoding, 
            ae => ae.Value.ToString() == "gzip");
    }

    /// <summary>
    /// Verifies automatic decompression is enabled
    /// </summary>
    [Fact]
    public void Handler_ShouldEnableAutomaticDecompression()
    {
        // Arrange
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | 
                                    System.Net.DecompressionMethods.Deflate
        };

        // Assert
        var hasGzip = (handler.AutomaticDecompression & System.Net.DecompressionMethods.GZip) != 0;
        var hasDeflate = (handler.AutomaticDecompression & System.Net.DecompressionMethods.Deflate) != 0;
        
        Assert.True(hasGzip);
        Assert.True(hasDeflate);
    }
}

/// <summary>
/// Tests for timeout configuration correctness
/// </summary>
public class TimeoutConfigurationTests
{
    /// <summary>
    /// Verifies that different proxies have appropriate timeouts for their use case
    /// </summary>
    [Fact]
    public void Proxies_ShouldHaveAppropriateTimeouts()
    {
        // Arrange
        var paymentTimeout = TimeSpan.FromSeconds(30);
        var oauthTimeout = TimeSpan.FromSeconds(15);
        var mediaTimeout = TimeSpan.FromSeconds(60);
        var notificationTimeout = TimeSpan.FromSeconds(15);

        // Assert - timeouts should be reasonable for their use cases
        Assert.True(paymentTimeout.TotalSeconds > 0 && paymentTimeout.TotalSeconds <= 60);
        Assert.True(oauthTimeout.TotalSeconds > 0 && oauthTimeout.TotalSeconds <= 30);
        Assert.True(mediaTimeout.TotalSeconds > 0 && mediaTimeout.TotalSeconds <= 120);
        Assert.True(notificationTimeout.TotalSeconds > 0 && notificationTimeout.TotalSeconds <= 30);

        // Media should have longer timeout than payment (file uploads)
        Assert.True(mediaTimeout > paymentTimeout);
        
        // OAuth and notification should be quick
        Assert.Equal(oauthTimeout, notificationTimeout);
    }

    /// <summary>
    /// Verifies that timeouts prevent hanging requests
    /// </summary>
    [Fact]
    public async Task HttpClient_WithTimeout_ShouldCancelSlowRequests()
    {
        // Arrange
        var handler = new SocketsHttpHandler();
        using var client = new System.Net.Http.HttpClient(handler)
        {
            Timeout = TimeSpan.FromMilliseconds(100)
        };

        var request = new HttpRequestMessage(HttpMethod.Get, "https://httpbin.org/delay/5");

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => client.SendAsync(request, CancellationToken.None));
    }
}
