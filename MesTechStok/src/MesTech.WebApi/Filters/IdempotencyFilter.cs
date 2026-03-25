using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace MesTech.WebApi.Filters;

/// <summary>
/// Idempotency key endpoint filter — duplicate POST/PUT işlemlerini önler.
/// X-Idempotency-Key header ile çalışır. Aynı key ile gelen ikinci istek
/// cache'ten önceki response'u döndürür, handler'a ulaşmaz.
///
/// Kullanım: .AddEndpointFilter&lt;IdempotencyFilter&gt;()
/// Cache TTL: 24 saat (ödeme/sipariş retry window'u)
/// </summary>
public sealed class IdempotencyFilter : IEndpointFilter
{
    private const string HeaderName = "X-Idempotency-Key";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        // Sadece state-changing method'larda çalış
        var method = httpContext.Request.Method;
        if (!HttpMethods.IsPost(method) && !HttpMethods.IsPut(method))
            return await next(context);

        // Header yoksa — normal devam (idempotency isteğe bağlı)
        var idempotencyKey = httpContext.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return await next(context);

        var cache = httpContext.RequestServices.GetRequiredService<IDistributedCache>();
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<IdempotencyFilter>>();
        var cacheKey = $"idempotency:{idempotencyKey}";

        // Cache'te var mı kontrol
        var cached = await cache.GetAsync(cacheKey, httpContext.RequestAborted);
        if (cached is not null)
        {
            logger.LogInformation(
                "Idempotency hit: key={Key}, returning cached response",
                idempotencyKey);

            var cachedResponse = JsonSerializer.Deserialize<IdempotencyCacheEntry>(cached);
            if (cachedResponse is not null)
            {
                httpContext.Response.StatusCode = cachedResponse.StatusCode;
                httpContext.Response.ContentType = "application/json";
                httpContext.Response.Headers["X-Idempotency-Replayed"] = "true";
                await httpContext.Response.WriteAsync(cachedResponse.Body, httpContext.RequestAborted);
                return null; // Short-circuit — handler'a ulaşmaz
            }
        }

        // İlk istek — handler'ı çalıştır
        var result = await next(context);

        // Response'u cache'le
        try
        {
            // IResult'tan status code ve body çıkar
            var statusCode = 200;
            var body = "{}";

            if (result is IStatusCodeHttpResult statusResult)
                statusCode = statusResult.StatusCode ?? 200;

            body = JsonSerializer.Serialize(result);

            var entry = new IdempotencyCacheEntry(statusCode, body);
            var entryBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(entry));

            await cache.SetAsync(cacheKey, entryBytes, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl
            }, httpContext.RequestAborted);

            logger.LogDebug("Idempotency cached: key={Key}, status={Status}", idempotencyKey, statusCode);
        }
        catch (Exception ex)
        {
            // Cache failure → request still succeeds, just not cached
            logger.LogWarning(ex, "Idempotency cache write failed for key={Key}", idempotencyKey);
        }

        return result;
    }

    private sealed record IdempotencyCacheEntry(int StatusCode, string Body);
}
