// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.API.Middleware;

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using KromicStore.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Middleware for automatic audit logging of HTTP requests.
/// Logs all requests with tenant context, user information, and response details.
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;
    private readonly IAuditLogService _auditLogService;

    public AuditLoggingMiddleware(
        RequestDelegate next,
        ILogger<AuditLoggingMiddleware> logger,
        IAuditLogService auditLogService)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip audit logging for health checks and static files
        if (ShouldSkipAuditLogging(context))
        {
            await _next(context);
            return;
        }

        // Store original body streams
        var originalRequestBody = context.Request.Body;
        var originalResponseBody = context.Response.Body;

        try
        {
            // Enable request body buffering
            context.Request.EnableBuffering();

            // Read request body
            string requestBody = await ReadRequestBodyAsync(context);

            // Create response body buffer
            using var responseBodyBuffer = new MemoryStream();
            context.Response.Body = responseBodyBuffer;

            // Extract context information
            var correlationId = GetCorrelationId(context);
            var tenantId = GetTenantId(context);
            var userId = GetUserId(context);
            var userType = GetUserType(context);
            var ipAddress = GetIpAddress(context);
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            // Continue processing the request
            await _next(context);

            // Read response body
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            string responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            // Copy response back to original stream
            await responseBodyBuffer.CopyToAsync(originalResponseBody);

            // Log the action
            await LogActionAsync(
                context,
                tenantId,
                userId,
                userType,
                ipAddress,
                userAgent,
                correlationId,
                requestBody,
                responseBody,
                context.Response.StatusCode);
        }
        finally
        {
            // Restore original streams
            context.Request.Body = originalRequestBody;
            context.Response.Body = originalResponseBody;
        }
    }

    private static bool ShouldSkipAuditLogging(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        
        // Skip health checks
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
            return true;

        // Skip static files
        if (path.StartsWith("/static", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/images", StringComparison.OrdinalIgnoreCase))
            return true;

        // Skip favicon
        if (path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static async Task<string> ReadRequestBodyAsync(HttpContext context)
    {
        context.Request.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Seek(0, SeekOrigin.Begin);
        return body;
    }

    private static string? GetCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
            return correlationId.ToString();
        return context.TraceIdentifier;
    }

    private static Guid? GetTenantId(HttpContext context)
    {
        if (context.Items.TryGetValue("TenantId", out var tenantIdObj) && tenantIdObj is Guid tenantId)
            return tenantId;
        return null;
    }

    private static Guid? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User?.FindFirst("sub")?.Value 
            ?? context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static string? GetUserType(HttpContext context)
    {
        if (context.User?.HasClaim(c => c.Type == "type" && c.Value == "superuser") == true)
            return "SuperUser";
        if (context.User?.Identity?.IsAuthenticated == true)
            return "User";
        return null;
    }

    private static string? GetIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString();
    }

    private async Task LogActionAsync(
        HttpContext context,
        Guid? tenantId,
        Guid? userId,
        string? userType,
        string? ipAddress,
        string? userAgent,
        string? correlationId,
        string requestBody,
        string responseBody,
        int statusCode)
    {
        try
        {
            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? string.Empty;
            
            // Determine action type based on HTTP method
            var action = method switch
            {
                "POST" => "Create",
                "PUT" => "Update",
                "PATCH" => "Update",
                "DELETE" => "Delete",
                "GET" => "View",
                _ => method
            };

            // Determine entity type from path
            var entityType = ExtractEntityTypeFromPath(path);

            // Extract entity ID from path if present
            var entityId = ExtractEntityIdFromPath(path);

            // Prepare metadata
            var metadata = JsonSerializer.Serialize(new
            {
                Method = method,
                Path = path,
                QueryString = context.Request.QueryString.ToString(),
                StatusCode = statusCode
            });

            // Determine success based on status code
            var success = statusCode < 400;

            if (success)
            {
                await _auditLogService.LogActionAsync(
                    tenantId,
                    userId,
                    userType,
                    entityType,
                    entityId,
                    action,
                    ipAddress,
                    userAgent,
                    correlationId,
                    oldState: FormatBodyForLog(requestBody),
                    newState: FormatBodyForLog(responseBody),
                    metadata: metadata);
            }
            else
            {
                await _auditLogService.LogFailureAsync(
                    tenantId,
                    userId,
                    userType,
                    entityType,
                    entityId,
                    action,
                    $"HTTP {statusCode}",
                    ipAddress,
                    userAgent,
                    correlationId,
                    metadata);
            }
        }
        catch (Exception ex)
        {
            // Log failure but don't break the request pipeline
            _logger.LogError(ex, "Failed to log audit entry for request");
        }
    }

    private static string ExtractEntityTypeFromPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 2)
        {
            // Extract entity name from path (e.g., /api/v1/products -> Product)
            var entitySegment = segments[^1];
            // Handle plural forms
            if (entitySegment.EndsWith("s", StringComparison.OrdinalIgnoreCase) && entitySegment.Length > 1)
            {
                return entitySegment[..^1]; // Remove trailing 's'
            }
            return entitySegment;
        }
        return "Unknown";
    }

    private static Guid? ExtractEntityIdFromPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (Guid.TryParse(segment, out var guid))
                return guid;
        }
        return null;
    }

    private static string? FormatBodyForLog(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        // Truncate large bodies to avoid excessive storage
        const int maxLength = 10000;
        if (body.Length > maxLength)
            return body[..maxLength] + "... (truncated)";

        return body;
    }
}
