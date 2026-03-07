namespace MesTech.Application.Interfaces;

/// <summary>
/// Token onbellegi soyutlamasi — in-memory veya Redis impl degistirilebilir.
/// </summary>
public interface ITokenCacheProvider
{
    Task<AuthToken?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(string key, AuthToken token, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}
