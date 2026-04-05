using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace MesTech.WebApi.Filters;

/// <summary>
/// Idempotency key endpoint filter — duplicate POST/PUT işlemlerini önler.
/// X-Idempotency-Key header ile çalışır. Aynı key ile gelen ikinci istek
/// cache'ten önceki response'u döndürür, handler'a ulaşmaz.
///
/// FIX-ÖZ-DENETİM (G040): IResult serialize edilemez — IValueHttpResult + IStatusCodeHttpResult
/// ile value ve status code çıkarılır. Response body buffering yerine IResult decomposition.
/// </summary>
public sealed class IdempotencyFilter : IEndpointFilter
{
    private const string HeaderName = "X-Idempotency-Key";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        var method = httpContext.Request.Method;
        if (!HttpMethods.IsPost(method) && !HttpMethods.IsPut(method))
            return await next(context).ConfigureAwait(false);

        var idempotencyKey = httpContext.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return await next(context).ConfigureAwait(false);

        var cache = httpContext.RequestServices.GetRequiredService<IDistributedCache>();
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<IdempotencyFilter>>();
        var cacheKey = $"idempotency:{idempotencyKey}";

        // Cache hit → replay
        var cached = await cache.GetAsync(cacheKey, httpContext.RequestAborted).ConfigureAwait(false);
        if (cached is not null)
        {
            logger.LogInformation("Idempotency hit: key={Key}, returning cached response", idempotencyKey);

            try
            {
                var cachedResponse = JsonSerializer.Deserialize<IdempotencyCacheEntry>(cached, JsonOptions);
                if (cachedResponse is not null)
                {
                    httpContext.Response.StatusCode = cachedResponse.StatusCode;
                    httpContext.Response.ContentType = cachedResponse.ContentType;
                    httpContext.Response.Headers["X-Idempotency-Replayed"] = "true";
                    await httpContext.Response.WriteAsync(cachedResponse.Body, httpContext.RequestAborted).ConfigureAwait(false);
                    return null;
                }
            }
            catch (JsonException ex)
            {
                // OWASP A08 guard: corrupted/poisoned cache entry — discard and re-execute
                logger.LogWarning(ex, "Idempotency cache corrupted for key={Key}, discarding", idempotencyKey);
                await cache.RemoveAsync(cacheKey, httpContext.RequestAborted).ConfigureAwait(false);
            }
        }

        // Cache miss → execute handler
        var result = await next(context).ConfigureAwait(false);

        // Cache the response — extract value from IResult properly
        try
        {
            var statusCode = 200;
            string body;

            // FIX-G040: IResult'tan value ve status code doğru çıkarma
            if (result is IStatusCodeHttpResult statusResult)
                statusCode = statusResult.StatusCode ?? 200;

            if (result is IValueHttpResult valueResult && valueResult.Value is not null)
            {
                // IValueHttpResult.Value → gerçek response nesnesi (DTO, ApiResponse<T> vb.)
                body = JsonSerializer.Serialize(valueResult.Value, valueResult.Value.GetType(), JsonOptions);
            }
            else if (result is not null and not IResult)
            {
                // Handler doğrudan object döndürdüyse (IResult değilse)
                body = JsonSerializer.Serialize(result, result.GetType(), JsonOptions);
            }
            else
            {
                // IResult ama IValueHttpResult değil (Results.NoContent gibi)
                body = "{}";
            }

            // G135 FIX: Detect Content-Type from response (set by middleware/result executor)
            // instead of always defaulting to application/json.
            var contentType = httpContext.Response.ContentType ?? "application/json";
            var entry = new IdempotencyCacheEntry(statusCode, body, contentType);
            var entryBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(entry, JsonOptions));

            await cache.SetAsync(cacheKey, entryBytes, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl
            }, httpContext.RequestAborted).ConfigureAwait(false);

            logger.LogDebug("Idempotency cached: key={Key}, status={Status}, bodyLen={Len}",
                idempotencyKey, statusCode, body.Length);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Idempotency cache write failed for key={Key}", idempotencyKey);
        }

        return result;
    }

    private sealed record IdempotencyCacheEntry(int StatusCode, string Body, string ContentType = "application/json");
}
