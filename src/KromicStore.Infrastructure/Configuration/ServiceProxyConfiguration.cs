// Copyright (c) KromicStore. All rights reserved.

namespace KromicStore.Infrastructure.Configuration
{
    /// <summary>
    /// Configuration for external service proxies including timeout, retry, and circuit breaker settings.
    /// </summary>
    public class ServiceProxyConfiguration
    {
        /// <summary>
        /// Gets or sets the connection timeout in seconds. Default: 30 seconds.
        /// </summary>
        public int ConnectionTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the request timeout in seconds. Default: 30 seconds.
        /// </summary>
        public int RequestTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts. Default: 4.
        /// </summary>
        public int MaxRetryCount { get; set; } = 4;

        /// <summary>
        /// Gets or sets the retry delays in milliseconds.
        /// Default: [100, 1000, 10000, 30000]
        /// </summary>
        public int[] RetryDelaysMs { get; set; } = new[] { 100, 1000, 10000, 30000 };

        /// <summary>
        /// Gets or sets the circuit breaker failure threshold. Default: 5.
        /// </summary>
        public int CircuitBreakerThreshold { get; set; } = 5;

        /// <summary>
        /// Gets or sets the circuit breaker timeout in seconds. Default: 30 seconds.
        /// </summary>
        public int CircuitBreakerTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets the circuit breaker timeout as a TimeSpan.
        /// </summary>
        public TimeSpan CircuitBreakerTimeout => TimeSpan.FromSeconds(CircuitBreakerTimeoutSeconds);

        /// <summary>
        /// Gets the total request timeout (connection + request timeout) as a TimeSpan.
        /// </summary>
        public TimeSpan TotalTimeout => TimeSpan.FromSeconds(ConnectionTimeoutSeconds + RequestTimeoutSeconds);
    }
}
