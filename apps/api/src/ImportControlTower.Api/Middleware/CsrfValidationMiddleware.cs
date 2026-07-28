using System.Net;

namespace ImportControlTower.Api.Middleware;

public class CsrfValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _allowedOrigins;

    public CsrfValidationMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        var originsSetting = configuration["ALLOWED_ORIGINS"] ?? "http://localhost:3000";
        _allowedOrigins = originsSetting
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(o => o.TrimEnd('/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Apply CSRF validation only on state-changing cookie auth endpoints
        if (path.EndsWith("/api/v1/auth/refresh") || 
            path.EndsWith("/api/v1/auth/logout") || 
            path.EndsWith("/api/v1/auth/logout-all"))
        {
            // 1. Check X-ICT-CSRF-Protection header
            if (!context.Request.Headers.TryGetValue("X-ICT-CSRF-Protection", out var csrfHeader) || csrfHeader != "1")
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"type\":\"https://tools.ietf.org/html/rfc7231#section-6.5.3\",\"title\":\"Forbidden\",\"status\":403,\"detail\":\"Missing or invalid X-ICT-CSRF-Protection header.\"}");
                return;
            }

            // 2. Check Origin / Referer against ALLOWED_ORIGINS
            string? clientOrigin = null;

            if (context.Request.Headers.TryGetValue("Origin", out var originValues) && !string.IsNullOrEmpty(originValues))
            {
                clientOrigin = originValues.ToString().TrimEnd('/');
            }
            else if (context.Request.Headers.TryGetValue("Referer", out var refererValues) && !string.IsNullOrEmpty(refererValues))
            {
                if (Uri.TryCreate(refererValues.ToString(), UriKind.Absolute, out var uri))
                {
                    clientOrigin = $"{uri.Scheme}://{uri.Authority}".TrimEnd('/');
                }
            }

            if (string.IsNullOrEmpty(clientOrigin) || !_allowedOrigins.Contains(clientOrigin))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"type\":\"https://tools.ietf.org/html/rfc7231#section-6.5.3\",\"title\":\"Forbidden\",\"status\":403,\"detail\":\"Invalid or untrusted origin.\"}");
                return;
            }
        }

        await _next(context);
    }
}
