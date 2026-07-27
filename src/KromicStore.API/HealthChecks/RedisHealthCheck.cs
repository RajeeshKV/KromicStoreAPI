using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace KromicStore.API.HealthChecks
{
    /// <summary>
    /// Health check for Redis cache connectivity.
    /// Attempts a simple PING command to verify Redis is accessible.
    /// </summary>
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ILogger<RedisHealthCheck> _logger;
        private const int TimeoutSeconds = 10;

        public RedisHealthCheck(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisHealthCheck> logger)
        {
            _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                // Log connection status and endpoints
                var endpoints = _connectionMultiplexer.GetEndPoints();
                _logger.LogInformation("Redis Health Check - IsConnected: {IsConnected}, EndpointCount: {Count}, Endpoints: {Endpoints}", 
                    _connectionMultiplexer.IsConnected, 
                    endpoints.Length, 
                    string.Join(", ", endpoints.Select(e => e.ToString())));

                // Check if connected
                if (!_connectionMultiplexer.IsConnected)
                {
                    return HealthCheckResult.Unhealthy(
                        "Redis connection is not established");
                }

                if (endpoints.Length == 0)
                {
                    return HealthCheckResult.Unhealthy(
                        "No Redis endpoints configured");
                }

                var server = _connectionMultiplexer.GetServer(endpoints.First());
                _logger.LogInformation("Redis Health Check - Server endpoint: {Endpoint}", server.EndPoint.ToString());

                // Execute PING command with timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

                var pingTask = server.PingAsync();
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(TimeoutSeconds), linkedCts.Token);
                
                var completedTask = await Task.WhenAny(pingTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    _logger.LogWarning("Redis Health Check - PING timed out after {Seconds} seconds", TimeoutSeconds);
                    return HealthCheckResult.Unhealthy(
                        $"Redis health check timed out after {TimeoutSeconds} seconds");
                }

                var pingResult = await pingTask;
                stopwatch.Stop();

                var data = new Dictionary<string, object>
                {
                    { "CacheType", "Redis" },
                    { "ResponseTime", $"{stopwatch.ElapsedMilliseconds}ms" },
                    { "Connected", _connectionMultiplexer.IsConnected },
                    { "Endpoints", string.Join(", ", endpoints.Select(e => e.ToString())) }
                };

                _logger.LogInformation("Redis Health Check - Success in {Ms}ms", stopwatch.ElapsedMilliseconds);
                return HealthCheckResult.Healthy(
                    "Redis cache is accessible",
                    data: data);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Redis Health Check - Operation cancelled");
                return HealthCheckResult.Unhealthy(
                    $"Redis health check timed out after {TimeoutSeconds} seconds");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis Health Check - Failed with exception");
                return HealthCheckResult.Unhealthy(
                    $"Redis health check failed: {ex.Message}",
                    exception: ex);
            }
        }
    }
}
