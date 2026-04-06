using System.Text.Json;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Auth;

/// <summary>
/// Redis-backed token cache — survives process restarts.
/// Prefix: "token:" ile token key'leri izole edilir.
/// TTL: Token ExpiresAt'e gore otomatik ayarlanir.
/// Fallback: Redis down ise LogWarning + InMemory devam eder (circuit breaker degil).
/// DEV3 TUR7: InMemoryTokenCacheProvider → Redis swap.
/// </summary>
public sealed class RedisTokenCacheProvider : ITokenCacheProvider
{
    private const string KeyPrefix = "token:";
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisTokenCacheProvider> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisTokenCacheProvider(
        IDistributedCache cache,
        ILogger<RedisTokenCacheProvider> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<AuthToken?> GetAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var json = await _cache.GetStringAsync(KeyPrefix + key, ct).ConfigureAwait(false);
            if (json is null) return null;

            return JsonSerializer.Deserialize<AuthToken>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Redis token cache GET failed for key={Key}, returning null", key);
            return null;
        }
    }

    public async Task SetAsync(string key, AuthToken token, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(token, JsonOptions);
            var options = new DistributedCacheEntryOptions();

            // TTL = token expiry + 5 min buffer (token yenileme suresi)
            if (token.ExpiresAt > DateTime.UtcNow)
            {
                options.AbsoluteExpiration = new DateTimeOffset(token.ExpiresAt.AddMinutes(5), TimeSpan.Zero);
            }
            else
            {
                // Expired token — 1 dk cache (refresh cycle icin)
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            }

            await _cache.SetStringAsync(KeyPrefix + key, json, options, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Redis token cache SET failed for key={Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _cache.RemoveAsync(KeyPrefix + key, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Redis token cache REMOVE failed for key={Key}", key);
        }
    }
}
