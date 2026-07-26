#nullable disable

using KromicStore.API.Handlers;
using KromicStore.Infrastructure.Proxies;
using KromicStore.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KromicStore.API.Extensions;

/// <summary>
/// Extension methods for registering and configuring HttpClient factories with resilience policies
/// Includes logging, compression, connection pooling, and timeout configuration per service
/// </summary>
public static class HttpClientServiceCollectionExtensions
{
    /// <summary>
    /// Configures all external service HttpClient factories with proper timeouts, handlers, and policies
    /// </summary>
    public static IServiceCollection AddExternalServiceHttpClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var externalServicesConfig = configuration.GetSection("ExternalServices");
        var defaultTimeoutSeconds = externalServicesConfig.GetValue("RequestTimeoutSeconds", 30);

        // Razorpay Service (Direct API calls for subscriptions and payments)
        // Timeout: 30 seconds (payments are typically quick)
        services.AddHttpClient<RazorpayService>(nameof(RazorpayService), client =>
            {
                client.BaseAddress = new Uri("https://api.razorpay.com/v1/");
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "KromicStore/1.0");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                    MaxConnectionsPerServer = 10,
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip | 
                                            System.Net.DecompressionMethods.Deflate
                };
            });

        // Payment Proxy (Razorpay)
        // Timeout: 30 seconds (payments are typically quick)
        services.AddHttpClient<PaymentProxy>(nameof(PaymentProxy), client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "KromicStore/1.0");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                    MaxConnectionsPerServer = 10,
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip | 
                                            System.Net.DecompressionMethods.Deflate
                };
            });

        // OAuth Proxy (Google)
        // Timeout: 15 seconds (token exchange should be quick)
        services.AddHttpClient<OAuthProxy>(nameof(OAuthProxy), client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.Add("User-Agent", "KromicStore/1.0");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                    MaxConnectionsPerServer = 5,
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip | 
                                            System.Net.DecompressionMethods.Deflate
                };
            });

        // Media Proxy (Cloudinary)
        // Timeout: 60 seconds (file uploads can take longer)
        services.AddHttpClient<MediaProxy>(nameof(MediaProxy), client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
                client.DefaultRequestHeaders.Add("User-Agent", "KromicStore/1.0");
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                    MaxConnectionsPerServer = 20,  // More connections for concurrent uploads
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip | 
                                            System.Net.DecompressionMethods.Deflate
                };
            });

        // Notification Proxy (Brevo)
        // Timeout: 15 seconds (email sending should be quick)
        services.AddHttpClient<NotificationProxy>(nameof(NotificationProxy), client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.Add("User-Agent", "KromicStore/1.0");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                    MaxConnectionsPerServer = 10,
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip | 
                                            System.Net.DecompressionMethods.Deflate
                };
            });

        return services;
    }
}
