namespace KromicStore.API.Middleware;

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

/// <summary>
/// Provides idempotent handling for authenticated API mutation requests.
/// </summary>
public sealed class IdempotencyMiddleware
{
    private static readonly HashSet<string> MutationMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete
    };

    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldApply(context))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault()
            ?? context.Request.Headers["X-Idempotency-Key"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            context.Response.StatusCode = StatusCodes.Status428PreconditionRequired;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Idempotency key is required for mutation requests.",
                errorCode = "IDEMPOTENCY_KEY_REQUIRED"
            });
            return;
        }

        var tenantId = context.User.FindFirst("tenant_id")?.Value ?? "platform";
        var requestHash = await ComputeRequestHashAsync(context.Request);
        var cacheKey = $"idempotency:{tenantId}:{context.Request.Method}:{context.Request.Path}:{idempotencyKey}";

        if (_cache.TryGetValue<IdempotencyCacheEntry>(cacheKey, out var cached) && cached is not null)
        {
            if (!string.Equals(cached.RequestHash, requestHash, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Idempotency key was reused with a different request payload.",
                    errorCode = "IDEMPOTENCY_KEY_REUSED"
                });
                return;
            }

            context.Response.StatusCode = cached.StatusCode;
            context.Response.ContentType = cached.ContentType;
            context.Response.Headers["X-Idempotency-Replayed"] = "true";
            await context.Response.WriteAsync(cached.Body);
            return;
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);

            buffer.Position = 0;
            var responseBody = await new StreamReader(buffer).ReadToEndAsync();

            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                _cache.Set(cacheKey, new IdempotencyCacheEntry(
                    requestHash,
                    context.Response.StatusCode,
                    context.Response.ContentType ?? "application/json",
                    responseBody), TimeSpan.FromHours(24));
            }

            context.Response.Body = originalBody;
            context.Response.Headers["X-Idempotency-Key"] = idempotencyKey;
            await context.Response.WriteAsync(responseBody);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private static bool ShouldApply(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/api/v1", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!MutationMethods.Contains(context.Request.Method))
            return false;

        if (path.StartsWith("/api/v1/auth", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/v1/superuser/auth", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/v1/public", StringComparison.OrdinalIgnoreCase))
            return false;

        return context.User.Identity?.IsAuthenticated == true;
    }

    private static async Task<string> ComputeRequestHashAsync(HttpRequest request)
    {
        request.EnableBuffering();
        var methodAndPath = Encoding.UTF8.GetBytes($"{request.Method}:{request.Path}:{request.QueryString}:");
        await using var bodyBuffer = new MemoryStream();
        await request.Body.CopyToAsync(bodyBuffer);
        request.Body.Position = 0;

        var bodyBytes = bodyBuffer.ToArray();
        var input = new byte[methodAndPath.Length + bodyBytes.Length];
        Buffer.BlockCopy(methodAndPath, 0, input, 0, methodAndPath.Length);
        Buffer.BlockCopy(bodyBytes, 0, input, methodAndPath.Length, bodyBytes.Length);
        return Convert.ToHexString(SHA256.HashData(input));
    }

    private sealed record IdempotencyCacheEntry(string RequestHash, int StatusCode, string ContentType, string Body);
}