using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Tüm platformlar için periyodik stok senkronizasyonu.
/// Platform kodu parametre olarak alınır — her platform ayrı Hangfire recurring job.
///
/// Pattern: Pull (platform stok) → lokal stok ile karşılaştır → delta varsa Push
///
/// Frekanslar (HangfireConfig'de tanımlanır):
///   HB, ÇS, N11, Pazarama:           30dk
///   Amazon, eBay, Shopify, WC:         1 saat
///   Ozon, Etsy, Zalando, PttAVM:       2 saat
/// </summary>
[AutomaticRetry(Attempts = 3)]
public sealed class GenericPlatformStockSyncJob
{
    private readonly IAdapterFactory _factory;
    private readonly ILogger<GenericPlatformStockSyncJob> _logger;

    public GenericPlatformStockSyncJob(
        IAdapterFactory factory,
        ILogger<GenericPlatformStockSyncJob> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <summary>
    /// Belirtilen platform için stok senkronizasyonu çalıştırır.
    /// Hangfire RecurringJob tarafından platform kodu ile çağrılır.
    /// </summary>
    public async Task ExecuteAsync(string platformCode, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[GenericSync] {Platform} stok sync başlıyor...", platformCode);

        var adapter = _factory.Resolve(platformCode);
        if (adapter is null)
        {
            _logger.LogWarning(
                "[GenericSync] {Platform} adapter bulunamadı — atlaniyor", platformCode);
            return;
        }

        if (!adapter.SupportsStockUpdate)
        {
            _logger.LogDebug(
                "[GenericSync] {Platform} stok güncelleme desteklemiyor — skip", platformCode);
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
                    var pushed = await adapter.PushStockUpdateAsync(
                        product.Id, product.Stock, ct).ConfigureAwait(false);

                    if (pushed) synced++;
                    else skipped++;
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogWarning(ex,
                        "[GenericSync] {Platform} stok push hatası — SKU={SKU}",
                        platformCode, product.SKU);
                }
            }

            _logger.LogInformation(
                "[GenericSync] {Platform} TAMAMLANDI — synced={Synced}, skipped={Skip}, errors={Err}, total={Total}",
                platformCode, synced, skipped, errors, products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[GenericSync] {Platform} stok sync BAŞARISIZ", platformCode);
            throw; // Hangfire retry
        }
    }
}
