using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior — ICacheableQuery implement eden query'lerin sonuçlarını
/// IMemoryCache'de otomatik cache'ler.
///
/// Kullanım: Query record'una ICacheableQuery ekle:
///   public record GetDashboardSummaryQuery(Guid TenantId) : IRequest&lt;DashboardDto&gt;, ICacheableQuery
///   {
///       public string CacheKey => $"Dashboard_{TenantId}";
///   }
///
/// Cache invalidation: Command handler'lar ilgili cache key'i temizler:
///   _cache.Remove("Dashboard_" + tenantId);
/// </summary>
public sealed class CacheBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheBehavior<TRequest, TResponse>> _logger;

    public CacheBehavior(IMemoryCache cache, ILogger<CacheBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Sadece ICacheableQuery implement eden request'ler cache'lenir
        if (request is not ICacheableQuery cacheableQuery)
            return await next().ConfigureAwait(false);

        var cacheKey = cacheableQuery.CacheKey;

        // Cache'de varsa direkt dön — DB'ye gitmeden
        if (_cache.TryGetValue(cacheKey, out TResponse? cachedResponse) && cachedResponse is not null)
        {
            _logger.LogDebug("[Cache] HIT — {RequestType} key={CacheKey}",
                typeof(TRequest).Name, cacheKey);
            return cachedResponse;
        }

        // Cache'de yoksa handler'ı çalıştır
        var response = await next().ConfigureAwait(false);

        // Sonucu cache'le
        var duration = cacheableQuery.CacheDuration ?? TimeSpan.FromMinutes(5);
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(duration)
            .SetSize(1); // Size-based eviction desteği

        _cache.Set(cacheKey, response, cacheOptions);

        _logger.LogDebug("[Cache] MISS → SET — {RequestType} key={CacheKey} ttl={Duration}min",
            typeof(TRequest).Name, cacheKey, duration.TotalMinutes);

        return response;
    }
}
