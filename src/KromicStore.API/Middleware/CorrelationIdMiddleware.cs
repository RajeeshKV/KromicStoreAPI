using KromicStore.API.Configuration;
using Microsoft.Extensions.Options;

namespace KromicStore.API.Middleware;

/// <summary>
/// Middleware for generating and propagating correlation IDs for distributed tracing
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private readonly CorrelationIdOptions _options;

    /// <summary>
    /// Initializes a new instance of CorrelationIdMiddleware
    /// </summary>
    /// <param name="next">The next middleware in the pipeline</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="options">Configuration options for CorrelationIdMiddleware</param>
    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger,
        IOptions<CorrelationIdOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new CorrelationIdOptions();
    }

    /// <summary>
    /// Processes the HTTP request and adds correlation ID to the context
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Try to get existing correlation ID from request header
        var correlationId = context.Request.Headers[_options.CorrelationIdHeader].ToString();
        var isNewCorrelationId = false;

        if (string.IsNullOrEmpty(correlationId))
        {
            if (_options.GenerateIfMissing)
            {
                // Generate new correlation ID if not provided
                correlationId = Guid.NewGuid().ToString();
                isNewCorrelationId = true;
            }
            else
            {
                correlationId = "NONE";
            }
        }

        // Store correlation ID in HTTP context items for access in services
        context.Items["CorrelationId"] = correlationId;

        // Add correlation ID to response headers if configured
        if (_options.IncludeInResponse && !context.Response.HasStarted)
        {
            context.Response.Headers[_options.CorrelationIdHeader] = correlationId;
        }

        // Log request start with comprehensive information
        _logger.LogInformation(
            "Request started - Method: {Method}, Path: {Path}, CorrelationId: {CorrelationId}, IsNewCorrelationId: {IsNewCorrelationId}",
            context.Request.Method,
            context.Request.Path,
            correlationId,
            isNewCorrelationId);

        var startTime = DateTime.UtcNow;

        try
        {
            await _next(context);

            var elapsed = DateTime.UtcNow - startTime;
            
            // Log request completion with status code and timing
            _logger.LogInformation(
                "Request completed - Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, CorrelationId: {CorrelationId}, ElapsedMs: {ElapsedMs}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                correlationId,
                elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var elapsed = DateTime.UtcNow - startTime;
            
            // Log request failure with exception context and correlation ID
            _logger.LogError(
                ex,
                "Request failed - Method: {Method}, Path: {Path}, CorrelationId: {CorrelationId}, ExceptionType: {ExceptionType}, ElapsedMs: {ElapsedMs}",
                context.Request.Method,
                context.Request.Path,
                correlationId,
                ex.GetType().Name,
                elapsed.TotalMilliseconds);
            throw;
        }
    }
}
