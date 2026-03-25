namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Platform komisyon hesaplama servisi.
/// Varsayilan oranlar: Trendyol %15, Hepsiburada %18, N11 %12, Ciceksepeti %20.
/// rateProvider null ise (legacy) veya sonuc null ise fallback kullanir.
/// </summary>
public sealed class CommissionCalculationService : ICommissionCalculationService
{
    private static readonly Dictionary<string, decimal> _fallbackRates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Trendyol"] = 0.15m,
        ["Hepsiburada"] = 0.18m,
        ["N11"] = 0.12m,
        ["Ciceksepeti"] = 0.20m,
        ["Amazon"] = 0.15m,
        ["Pazarama"] = 0.10m
    };

    private readonly Func<string, string?, CancellationToken, Task<DynamicRateResult?>>? _rateProvider;
    private DynamicRateResult? _cachedRate;
    private string? _cachedPlatform;
    private string? _cachedCategory;

    /// <summary>
    /// Legacy constructor — sadece fallback oranlar kullanilir.
    /// </summary>
    public CommissionCalculationService()
    {
        _rateProvider = null;
    }

    /// <summary>
    /// Dinamik oran destekli constructor.
    /// rateProvider null olabilir — bu durumda fallback kullanilir.
    /// </summary>
    public CommissionCalculationService(
        Func<string, string?, CancellationToken, Task<DynamicRateResult?>>? rateProvider = null)
    {
        _rateProvider = rateProvider;
    }

    /// <inheritdoc />
    public decimal CalculateCommission(string platform, string? category, decimal grossAmount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platform);
        if (grossAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(grossAmount), "Gross amount must be non-negative.");

        var rate = GetDefaultRate(platform);
        return Math.Round(grossAmount * rate, 2);
    }

    /// <inheritdoc />
    public async Task<CommissionCalculationResult> CalculateCommissionAsync(
        string platform,
        string? category,
        decimal grossAmount,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platform);
        if (grossAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(grossAmount), "Gross amount must be non-negative.");

        // 1. rateProvider var mi?
        if (_rateProvider is not null)
        {
            // 2. Cache kontrolu — ayni platform/kategori ve suresi dolmamissa tekrar cagirma
            if (_cachedRate is not null
                && _cachedPlatform == platform
                && _cachedCategory == category
                && _cachedRate.CachedUntil > DateTime.UtcNow)
            {
                var cachedAmount = Math.Round(grossAmount * _cachedRate.Rate, 2);
                return new CommissionCalculationResult(
                    _cachedRate.Rate,
                    cachedAmount,
                    _cachedRate.Source,
                    IsCached: true);
            }

            // 3. Dinamik oran iste
            var dynamicRate = await _rateProvider(platform, category, cancellationToken)
                .ConfigureAwait(false);

            if (dynamicRate is not null)
            {
                if (dynamicRate.Rate < 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(platform),
                        $"Dynamic rate for platform '{platform}' returned negative value: {dynamicRate.Rate}");

                // Cache'e kaydet
                _cachedRate = dynamicRate;
                _cachedPlatform = platform;
                _cachedCategory = category;

                var amount = Math.Round(grossAmount * dynamicRate.Rate, 2);
                return new CommissionCalculationResult(
                    dynamicRate.Rate,
                    amount,
                    dynamicRate.Source,
                    IsCached: false);
            }
        }

        // 4. Fallback
        var fallbackRate = GetDefaultRate(platform);
        var fallbackAmount = Math.Round(grossAmount * fallbackRate, 2);
        return new CommissionCalculationResult(
            fallbackRate,
            fallbackAmount,
            "StaticFallback",
            IsCached: false);
    }

    /// <inheritdoc />
    public decimal GetDefaultRate(string platform)
    {
        return _fallbackRates.TryGetValue(platform, out var rate) ? rate : 0.15m;
    }
}
