using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Tüm platformlar için periyodik fiyat senkronizasyonu.
/// Platform kodu parametre olarak alınır — her platform ayrı Hangfire recurring job.
///
/// Pattern: PullProducts → lokal fiyat ile karşılaştır → delta varsa PushPriceUpdate
///
/// G499 FIX: Sadece TrendyolPriceSyncJob vardı. Diğer platformlardan fiyat
/// değişikliği algılanmıyordu → Buybox kaybı tespit edilemiyordu.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class GenericPlatformPriceSyncJob
{
    private readonly IAdapterFactory _factory;
    private readonly ILogger<GenericPlatformPriceSyncJob> _logger;

    public GenericPlatformPriceSyncJob(
        IAdapterFactory factory,
        ILogger<GenericPlatformPriceSyncJob> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task ExecuteAsync(string platformCode, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[PriceSync] {Platform} fiyat sync başlıyor...", platformCode);

        var adapter = _factory.Resolve(platformCode);
        if (adapter is null)
        {
            _logger.LogWarning(
                "[PriceSync] {Platform} adapter bulunamadı — atlaniyor", platformCode);
            return;
        }

        if (!adapter.SupportsPriceUpdate)
        {
            _logger.LogDebug(
                "[PriceSync] {Platform} fiyat güncelleme desteklemiyor — skip", platformCode);
            return;
        }

        try
        {
            var products = await adapter.PullProductsAsync(ct).ConfigureAwait(false);
            int synced = 0, skipped = 0, errors = 0;

            foreach (var product in products)
            {
                try
                {
                    var pushed = await adapter.PushPriceUpdateAsync(
                        product.Id, product.SalePrice, ct).ConfigureAwait(false);

                    if (pushed) synced++;
                    else skipped++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    errors++;
                    _logger.LogWarning(ex,
                        "[PriceSync] {Platform} fiyat push hatası — SKU={SKU}",
                        platformCode, product.SKU);
                }
            }

            _logger.LogInformation(
                "[PriceSync] {Platform} TAMAMLANDI — synced={Synced}, skipped={Skip}, errors={Err}, total={Total}",
                platformCode, synced, skipped, errors, products.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "[PriceSync] {Platform} fiyat sync BAŞARISIZ", platformCode);
            throw; // Hangfire retry
        }
    }
}
