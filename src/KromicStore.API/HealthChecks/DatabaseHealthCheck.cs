using Microsoft.Extensions.Diagnostics.HealthChecks;
using KromicStore.Infrastructure.Data;
using System.Diagnostics;

namespace KromicStore.API.HealthChecks
{
    /// <summary>
    /// Health check for database connectivity via Entity Framework Core.
    /// Attempts a simple SELECT 1 query to verify database is accessible.
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly IServiceProvider _serviceProvider;
        private const int TimeoutSeconds = 10;

        public DatabaseHealthCheck(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var stopwatch = Stopwatch.StartNew();

                // Use OperationTimeout to enforce a timeout on the query
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

                // Execute a simple SELECT 1 query to verify database connectivity
                var isConnected = await dbContext.Database.CanConnectAsync(linkedCts.Token);

                stopwatch.Stop();

                if (!isConnected)
                {
                    return HealthCheckResult.Unhealthy("Database connection failed");
                }

                var data = new Dictionary<string, object>
                {
                    { "DatabaseType", "PostgreSQL" },
                    { "ResponseTime", $"{stopwatch.ElapsedMilliseconds}ms" }
                };

                return HealthCheckResult.Healthy(
                    "Database is accessible",
                    data: data);
            }
            catch (OperationCanceledException)
            {
                return HealthCheckResult.Unhealthy(
                    $"Database health check timed out after {TimeoutSeconds} seconds");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    $"Database health check failed: {ex.Message}",
                    exception: ex);
            }
        }
    }
}
