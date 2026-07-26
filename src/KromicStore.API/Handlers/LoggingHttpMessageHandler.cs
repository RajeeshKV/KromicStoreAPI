using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace KromicStore.API.Handlers;

/// <summary>
/// HTTP message handler that logs all outgoing HTTP requests and incoming responses
/// Provides visibility into external service communication for debugging and monitoring
/// </summary>
public class LoggingHttpMessageHandler : DelegatingHandler
{
    private readonly ILogger<LoggingHttpMessageHandler> _logger;
    private const int MaxContentLengthToLog = 1000;  // Limit logged content size

    public LoggingHttpMessageHandler(ILogger<LoggingHttpMessageHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var requestId = Guid.NewGuid().ToString()[..8];
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Log request details
            LogRequest(request, requestId);

            // Send the request
            var response = await base.SendAsync(request, cancellationToken);

            stopwatch.Stop();

            // Log response details
            await LogResponse(response, requestId, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogException(request, ex, requestId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Logs outgoing HTTP request with method, URI, headers, and body preview
    /// </summary>
    private void LogRequest(HttpRequestMessage request, string requestId)
    {
        var method = request.Method.Method.ToUpper();
        var uri = request.RequestUri?.ToString() ?? "Unknown";
        
        var headers = new StringBuilder();
        foreach (var header in request.Headers)
        {
            // Mask sensitive headers
            var value = IsSensitiveHeader(header.Key)
                ? "***MASKED***"
                : string.Join(",", header.Value);
            headers.AppendLine($"  {header.Key}: {value}");
        }

        var bodyPreview = "No body";
        if (request.Content != null)
        {
            if (request.Content is StringContent || request.Content is FormUrlEncodedContent)
            {
                var contentStr = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                bodyPreview = contentStr.Length > MaxContentLengthToLog
                    ? $"{contentStr[..MaxContentLengthToLog]}... (truncated)"
                    : contentStr;
            }
            else if (request.Content is MultipartFormDataContent)
            {
                bodyPreview = $"MultipartFormData (Content-Length: {request.Content.Headers.ContentLength ?? 0} bytes)";
            }
            else
            {
                bodyPreview = $"{request.Content.GetType().Name} (Content-Length: {request.Content.Headers.ContentLength ?? 0} bytes)";
            }
        }

        _logger.LogInformation(
            "HTTP {Method} {Uri} | RequestId: {RequestId} | Headers:\n{Headers}Body: {Body}",
            method,
            uri,
            requestId,
            headers.Length > 0 ? headers.ToString() : "  (none)\n",
            bodyPreview);
    }

    /// <summary>
    /// Logs incoming HTTP response with status code, headers, and body preview
    /// </summary>
    private async Task LogResponse(HttpResponseMessage response, string requestId, long elapsedMs)
    {
        var statusCode = (int)response.StatusCode;
        var statusDescription = response.StatusCode.ToString();
        var isSuccess = response.IsSuccessStatusCode;

        var headers = new StringBuilder();
        foreach (var header in response.Headers)
        {
            var value = string.Join(",", header.Value);
            headers.AppendLine($"  {header.Key}: {value}");
        }

        // Add content headers
        if (response.Content?.Headers != null)
        {
            foreach (var header in response.Content.Headers)
            {
                var value = string.Join(",", header.Value);
                headers.AppendLine($"  {header.Key}: {value}");
            }
        }

        var bodyPreview = "No body";
        if (response.Content != null && response.Content.Headers.ContentLength > 0)
        {
            try
            {
                var contentStr = await response.Content.ReadAsStringAsync();
                bodyPreview = contentStr.Length > MaxContentLengthToLog
                    ? $"{contentStr[..MaxContentLengthToLog]}... (truncated)"
                    : contentStr;
            }
            catch
            {
                bodyPreview = "(unable to read)";
            }
        }

        var logLevel = isSuccess ? LogLevel.Information : LogLevel.Warning;
        
        _logger.Log(
            logLevel,
            "HTTP {StatusCode} {StatusDescription} | RequestId: {RequestId} | ElapsedMs: {ElapsedMs} | Headers:\n{Headers}Body: {Body}",
            statusCode,
            statusDescription,
            requestId,
            elapsedMs,
            headers.Length > 0 ? headers.ToString() : "  (none)\n",
            bodyPreview);
    }

    /// <summary>
    /// Logs exception that occurred during HTTP communication
    /// </summary>
    private void LogException(HttpRequestMessage request, Exception ex, string requestId, long elapsedMs)
    {
        var method = request.Method.Method.ToUpper();
        var uri = request.RequestUri?.ToString() ?? "Unknown";

        _logger.LogError(
            ex,
            "HTTP {Method} {Uri} failed | RequestId: {RequestId} | ElapsedMs: {ElapsedMs} | Exception: {Exception}",
            method,
            uri,
            requestId,
            elapsedMs,
            ex.Message);
    }

    /// <summary>
    /// Determines if a header should be masked in logs for security
    /// </summary>
    private static bool IsSensitiveHeader(string headerName)
    {
        var sensitiveHeaders = new[]
        {
            "Authorization",
            "X-API-Key",
            "X-API-Secret",
            "X-Auth-Token",
            "Cookie",
            "Set-Cookie",
            "Idempotency-Key"  // Can contain sensitive data
        };

        return sensitiveHeaders.Contains(headerName, StringComparer.OrdinalIgnoreCase);
    }
}
