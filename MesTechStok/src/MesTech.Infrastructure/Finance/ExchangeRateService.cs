using System.Xml.Linq;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Finance;

/// <summary>
/// TCMB XML API uzerinden doviz kuru servisi.
/// Dalga 11 — DEV 4: Multi-currency destek.
///
/// Kaynak: https://www.tcmb.gov.tr/kurlar/today.xml
/// Cache: IMemoryCache, 1 saat TTL.
/// Fallback: TCMB'ye ulasilamazsa sabit kurlar kullanilir.
/// </summary>
public sealed class ExchangeRateService : IExchangeRateService
{
    private const string TcmbUrl = "https://www.tcmb.gov.tr/kurlar/today.xml";
    private const string CacheKey = "tcmb:exchange_rates";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    /// <summary>
    /// Fallback kurlar — TCMB'ye ulasilamadiginda kullanilir.
    /// Son guncelleme: 2026-03-15
    /// </summary>
    private static readonly Dictionary<string, decimal> FallbackRates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = 33m,
        ["EUR"] = 36m,
        ["GBP"] = 42m,
    };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ExchangeRateService> _logger;

    public ExchangeRateService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<ExchangeRateService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<decimal> GetRateAsync(
        string fromCurrency,
        string toCurrency = "TRY",
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromCurrency);
        ArgumentException.ThrowIfNullOrWhiteSpace(toCurrency);

        var from = fromCurrency.Trim().ToUpperInvariant();
        var to = toCurrency.Trim().ToUpperInvariant();

        // TRY -> TRY = 1
        if (from == "TRY" && to == "TRY")
            return 1m;

        var rates = await GetRatesAsync(ct);

        // from -> TRY
        if (to == "TRY")
        {
            return rates.TryGetValue(from, out var rate)
                ? rate
                : throw new InvalidOperationException($"Doviz kuru bulunamadi: {from}/TRY");
        }

        // TRY -> to (ters kur)
        if (from == "TRY")
        {
            return rates.TryGetValue(to, out var rate) && rate != 0
                ? 1m / rate
                : throw new InvalidOperationException($"Doviz kuru bulunamadi: TRY/{to}");
        }

        // from -> to (capraz kur: from->TRY / to->TRY)
        if (rates.TryGetValue(from, out var fromRate) && rates.TryGetValue(to, out var toRate) && toRate != 0)
            return fromRate / toRate;

        throw new InvalidOperationException($"Doviz kuru bulunamadi: {from}/{to}");
    }

    /// <inheritdoc />
    public async Task<decimal> ConvertToTryAsync(
        decimal amount,
        string currency,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        if (currency.Trim().Equals("TRY", StringComparison.OrdinalIgnoreCase))
            return amount;

        var rate = await GetRateAsync(currency, "TRY", ct);
        return Math.Round(amount * rate, 2, MidpointRounding.AwayFromZero);
    }

    /// <inheritdoc />
    public void InvalidateCache()
    {
        _cache.Remove(CacheKey);
        _logger.LogInformation("TCMB doviz kuru cache gecersiz kilindi");
    }

    /// <summary>
    /// Cache'li kur sozlugunu doner. Cache miss'te TCMB'den cekilir.
    /// </summary>
    private async Task<Dictionary<string, decimal>> GetRatesAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue(CacheKey, out Dictionary<string, decimal>? cached) && cached is not null)
        {
            _logger.LogDebug("TCMB kur cache HIT ({Count} doviz)", cached.Count);
            return cached;
        }

        var rates = await FetchFromTcmbAsync(ct);

        _cache.Set(CacheKey, rates, CacheTtl);
        _logger.LogInformation("TCMB kurlari guncellendi: {Count} doviz, cache 1 saat", rates.Count);

        return rates;
    }

    /// <summary>
    /// TCMB XML API'den guncel kurlari cekilir.
    /// Basarisiz olursa fallback kurlar kullanilir.
    /// </summary>
    private async Task<Dictionary<string, decimal>> FetchFromTcmbAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("TCMB XML API'den kur cekilecek: {Url}", TcmbUrl);

            var response = await _httpClient.GetStringAsync(TcmbUrl, ct);
            var doc = XDocument.Parse(response);

            var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            var currencies = doc.Descendants("Currency");
            foreach (var currency in currencies)
            {
                var code = currency.Attribute("CurrencyCode")?.Value;
                if (string.IsNullOrWhiteSpace(code))
                    continue;

                var forexBuyingStr = currency.Element("ForexBuying")?.Value;
                if (string.IsNullOrWhiteSpace(forexBuyingStr))
                    continue;

                // TCMB XML uses dot as decimal separator
                if (decimal.TryParse(forexBuyingStr,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var rate) && rate > 0)
                {
                    rates[code] = rate;
                }
            }

            if (rates.Count == 0)
            {
                _logger.LogWarning("TCMB XML parse sonucu bos — fallback kurlara geciliyor");
                return new Dictionary<string, decimal>(FallbackRates, StringComparer.OrdinalIgnoreCase);
            }

            _logger.LogDebug("TCMB'den {Count} doviz kuru cekildi", rates.Count);
            return rates;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TCMB API baglantisi basarisiz — fallback kurlar kullanilacak (USD={USD}, EUR={EUR}, GBP={GBP})",
                FallbackRates["USD"], FallbackRates["EUR"], FallbackRates["GBP"]);

            return new Dictionary<string, decimal>(FallbackRates, StringComparer.OrdinalIgnoreCase);
        }
    }
}
