using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MesTech.Infrastructure.Middleware;

/// <summary>
/// Validates X-API-Key header for MesTech Web API endpoints (port 5100).
/// Bypass paths (/health, /metrics, /api/mesa/status) skip validation.
/// Returns 401 with plain-text reason on missing or invalid key.
/// IP-6 — KOPRU EMIRNAMESI: protects MesTech Web API from unauthorized access.
/// </summary>
public sealed class ApiKeyMiddleware(
    RequestDelegate next,
    IOptions<ApiKeyOptions> options,
    ILogger<ApiKeyMiddleware> logger)
{
    private readonly ApiKeyOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip check for health/metrics/status endpoints
        if (_options.BypassPaths.Any(p =>
                path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(_options.HeaderName, out var receivedKey)
            || string.IsNullOrWhiteSpace(receivedKey))
        {
            logger.LogWarning("API key missing. Path={Path} IP={IP}",
                path, context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API key required.");
            return;
        }

        if (!_options.ValidApiKeys.Contains(receivedKey.ToString()))
        {
            logger.LogWarning("Invalid API key. Path={Path} IP={IP}",
                path, context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid API key.");
            return;
        }

        await next(context);
    }
}
