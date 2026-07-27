using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace KromicStore.API;

/// <summary>
/// Swagger document filter to dynamically set server URL based on request headers.
/// Ensures HTTPS URLs are generated when behind reverse proxy (Render).
/// </summary>
public class SwaggerDocumentFilter : IDocumentFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SwaggerDocumentFilter(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            // Fallback to empty server URL if no context
            swaggerDoc.Servers = new List<OpenApiServer>
            {
                new OpenApiServer { Url = "/" }
            };
            return;
        }

        var scheme = httpContext.Request.Scheme;
        var host = httpContext.Request.Host.Value;

        // Use X-Forwarded-Proto if available (from reverse proxy like Render)
        if (httpContext.Request.Headers.ContainsKey("X-Forwarded-Proto"))
        {
            scheme = httpContext.Request.Headers["X-Forwarded-Proto"].ToString();
        }

        // Use X-Forwarded-Host if available
        if (httpContext.Request.Headers.ContainsKey("X-Forwarded-Host"))
        {
            host = httpContext.Request.Headers["X-Forwarded-Host"].ToString();
        }

        swaggerDoc.Servers = new List<OpenApiServer>
        {
            new OpenApiServer
            {
                Url = $"{scheme}://{host}"
            }
        };
    }
}
