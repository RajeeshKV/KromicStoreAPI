using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using System.Diagnostics;

namespace KromicStore.API.HealthChecks
{
    /// <summary>
    /// Health check for Redis cache connectivity.
    /// Attempts a simple PING command to verify Redis is accessible.
    /// </summary>
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private const int TimeoutSeconds = 5;

        public RedisHealthCheck(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                // Use CancellationToken for timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

                // Get first connected server
                var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().FirstOrDefault() 
                    ?? throw new InvalidOperationException("No Redis endpoints available"));

                // Execute PING command
                await server.PingAsync();

                stopwatch.Stop();

                var data = new Dictionary<string, object>
                {
                    { "CacheType", "Redis" },
                    { "ResponseTime", $"{stopwatch.ElapsedMilliseconds}ms" },
                    { "Connected", _connectionMultiplexer.IsConnected }
                };

                return HealthCheckResult.Healthy(
                    "Redis cache is accessible",
                    data: data);
            }
            catch (OperationCanceledException)
            {
                return HealthCheckResult.Unhealthy(
                    $"Redis health check timed out after {TimeoutSeconds} seconds");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    $"Redis health check failed: {ex.Message}",
                    exception: ex);
            }
        }
    }
}
