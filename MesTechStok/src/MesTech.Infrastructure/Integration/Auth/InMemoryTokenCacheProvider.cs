using System.Collections.Concurrent;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Integration.Auth;

/// <summary>
/// Thread-safe in-memory token cache.
/// Redis'e gecis icin sadece DI registration degistirilir.
/// </summary>
public sealed class InMemoryTokenCacheProvider : ITokenCacheProvider
{
    private readonly ConcurrentDictionary<string, AuthToken> _cache = new();

    public Task<AuthToken?> GetAsync(string key, CancellationToken ct = default)
    {
        _cache.TryGetValue(key, out var token);
        return Task.FromResult(token);
    }

    public Task SetAsync(string key, AuthToken token, CancellationToken ct = default)
    {
        _cache.AddOrUpdate(key, token, (_, _) => token);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
