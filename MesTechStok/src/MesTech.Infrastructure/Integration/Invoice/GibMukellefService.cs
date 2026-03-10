using MesTech.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Invoice;

/// <summary>
/// GIB mukellef sorgu servisi — tum fatura adapter'larin paylastigi cache'li VKN sorgusu.
/// Cache TTL: 24 saat (GIB registry gece guncellenir).
/// Fallback: hata durumunda false doner (e-Arsiv, guvenli B2C varsayimi).
/// </summary>
public class GibMukellefService : IGibMukellefService
{
    private const string CachePrefix = "gib:vkn:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private readonly IEnumerable<IInvoiceProvider> _providers;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GibMukellefService> _logger;

    public GibMukellefService(
        IEnumerable<IInvoiceProvider> providers,
        IMemoryCache cache,
        ILogger<GibMukellefService> logger)
    {
        _providers = providers;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsEFaturaMukellefAsync(string vknOrTckn, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(vknOrTckn))
            return false;

        var normalized = vknOrTckn.Trim().ToUpperInvariant();
        var cacheKey = $"{CachePrefix}{normalized}";

        if (_cache.TryGetValue(cacheKey, out bool cached))
        {
            _logger.LogDebug("GIB mukellef cache HIT: {VKN} -> {Result}", normalized, cached);
            return cached;
        }

        var result = await QueryProvidersAsync(normalized, ct);

        _cache.Set(cacheKey, result, CacheTtl);
        _logger.LogInformation("GIB mukellef sorgu: {VKN} -> {Result} (cached 24h)", normalized, result);

        return result;
    }

    public void ClearCache()
    {
        if (_cache is MemoryCache mc)
        {
            mc.Compact(1.0);
            _logger.LogWarning("GIB mukellef cache temizlendi (compact)");
        }
    }

    private async Task<bool> QueryProvidersAsync(string vkn, CancellationToken ct)
    {
        foreach (var provider in _providers)
        {
            try
            {
                var result = await provider.IsEInvoiceTaxpayerAsync(vkn, ct);
                _logger.LogDebug("GIB sorgu via {Provider}: {VKN} -> {Result}",
                    provider.ProviderName, vkn, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GIB sorgu basarisiz via {Provider}, sonraki deneniyor",
                    provider.ProviderName);
            }
        }

        _logger.LogWarning("GIB sorgu: tum provider'lar basarisiz, varsayilan false (e-Arsiv)");
        return false;
    }
}
