#nullable disable

using System.Text.Json;
using FluentValidation;
using KromicStore.API.Configuration;
using KromicStore.Application.Exceptions;
using KromicStore.Contracts.Abstractions;
using Microsoft.Extensions.Options;
using ApplicationException = KromicStore.Application.Exceptions.ApplicationException;

namespace KromicStore.API.Middleware;

/// <summary>
/// Middleware for centralized error handling and standardized error response formatting.
/// Catches all unhandled exceptions and maps them to appropriate HTTP status codes with standardized ErrorResponse DTOs.
/// Supports multi-tenancy by including tenant context if available.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly ErrorHandlingOptions _options;

    /// <summary>
    /// Initializes a new instance of ErrorHandlingMiddleware
    /// </summary>
    /// <param name="next">The next middleware in the pipeline</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="options">Configuration options for ErrorHandlingMiddleware</param>
    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IOptions<ErrorHandlingOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new ErrorHandlingOptions();
    }

    /// <summary>
    /// Processes the HTTP request and handles exceptions
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred. TraceId: {TraceId}", context.TraceIdentifier);
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Checks if the current path is in the bypass list
    /// </summary>
    private bool IsPathInBypassList(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;
        return _options.BypassPaths.Any(p => pathValue.StartsWith(p.ToLowerInvariant()));
    }

    /// <summary>
    /// Handles exceptions and returns standardized error response
    /// Maps exceptions to appropriate HTTP status codes:
    /// - ValidationException → 400 Bad Request
    /// - UnauthorizedAccessException → 401 Unauthorized
    /// - OperationCanceledException → 499 Client Closed Request
    /// - TimeoutException → 504 Gateway Timeout
    /// - ProxyException → 502 Bad Gateway or 503 Service Unavailable
    /// - ApplicationException → Status code from exception
    /// - Generic exceptions → 500 Internal Server Error
    /// </summary>
    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var correlationId = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;
        var tenantId = context.Items["TenantId"]?.ToString() ?? "UNKNOWN";

        var (statusCode, errorCode, message, details) = MapExceptionToResponse(exception);

        context.Response.StatusCode = statusCode;

        // Log exception with full context
        var stackTrace = _options.LogStackTraces ? exception.StackTrace : null;
        _logger.LogError(
            exception,
            "Exception handled - ExceptionType: {ExceptionType}, ErrorCode: {ErrorCode}, StatusCode: {StatusCode}, " +
            "Path: {Path}, TraceId: {TraceId}, CorrelationId: {CorrelationId}, TenantId: {TenantId}, " +
            "Message: {Message}, StackTrace: {StackTrace}",
            exception.GetType().Name,
            errorCode,
            statusCode,
            context.Request.Path,
            context.TraceIdentifier,
            correlationId,
            tenantId,
            exception.Message,
            stackTrace);

        // Use generic message for 500 errors if configured
        if (_options.UseGenericInternalErrorMessage && statusCode == 500 && !_options.IncludeExceptionDetails)
        {
            message = _options.GenericInternalErrorMessage;
        }

        var errorResponse = new ErrorResponse(
            errorCode,
            message,
            details
        );

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        return context.Response.WriteAsJsonAsync(errorResponse, jsonOptions);
    }

    /// <summary>
    /// Maps exceptions to HTTP status codes and error details
    /// </summary>
    /// <returns>Tuple of (statusCode, errorCode, message, details dictionary)</returns>
    private (int StatusCode, string ErrorCode, string Message, IDictionary<string, string[]> Details) 
        MapExceptionToResponse(Exception exception)
    {
        return exception switch
        {
            // FluentValidation ValidationException → 400 Bad Request
            FluentValidation.ValidationException validationEx =>
            (
                400,
                "VALIDATION_ERROR",
                "One or more validation failures occurred.",
                validationEx.Errors
                    .GroupBy(f => f.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(f => f.ErrorMessage).ToArray()
                    ) as IDictionary<string, string[]>
            ),

            // Application ValidationException → 400 Bad Request
            Application.Exceptions.ValidationException appValidationEx =>
            (
                400,
                "VALIDATION_ERROR",
                "One or more validation failures occurred.",
                appValidationEx.Errors as IDictionary<string, string[]>
            ),

            // UnauthorizedAccessException → 401 Unauthorized
            UnauthorizedAccessException =>
            (
                401,
                "UNAUTHORIZED",
                "Access is denied. Authentication is required.",
                null
            ),

            // OperationCanceledException → 499 Client Closed Request
            OperationCanceledException =>
            (
                499,
                "CLIENT_CLOSED_REQUEST",
                "The request was cancelled.",
                null
            ),

            // TimeoutException → 504 Gateway Timeout
            TimeoutException =>
            (
                504,
                "GATEWAY_TIMEOUT",
                "The operation timed out. Please try again.",
                null
            ),

            // ProxyException → 502 Bad Gateway or 503 Service Unavailable (from exception)
            ProxyException proxyEx =>
            (
                proxyEx.StatusCode,
                proxyEx.ErrorCode ?? "EXTERNAL_SERVICE_ERROR",
                proxyEx.Message,
                null
            ),

            // ApplicationException → Use status code from exception
            ApplicationException appEx =>
            (
                appEx.StatusCode,
                appEx.ErrorCode ?? "APPLICATION_ERROR",
                appEx.Message,
                null
            ),

            // DomainException → 400 Bad Request with domain error code
            DomainException domainEx =>
            (
                400,
                domainEx.ErrorCode ?? "DOMAIN_ERROR",
                domainEx.Message,
                null
            ),

            // Generic Exception → 500 Internal Server Error
            _ =>
            (
                500,
                "INTERNAL_SERVER_ERROR",
                _options.IncludeExceptionDetails ? exception.Message : _options.GenericInternalErrorMessage,
                null
            )
        };
    }
}
