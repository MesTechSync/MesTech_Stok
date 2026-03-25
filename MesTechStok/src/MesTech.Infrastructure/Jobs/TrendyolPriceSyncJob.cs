using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 6 saatte Trendyol ile fiyat esitlemesi yapar.
/// </summary>
[AutomaticRetry(Attempts = 3)]
public sealed class TrendyolPriceSyncJob : ISyncJob
{
    public string JobId => "trendyol-price-sync";
    public string CronExpression => "0 */6 * * *"; // Her 6 saat

    private readonly IAdapterFactory _factory;
    private readonly ILogger<TrendyolPriceSyncJob> _logger;

    public TrendyolPriceSyncJob(IAdapterFactory factory, ILogger<TrendyolPriceSyncJob> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Trendyol fiyat sync basliyor...", JobId);

        try
        {
            var adapter = _factory.Resolve("Trendyol");
            if (adapter == null)
            {
                _logger.LogWarning("[{JobId}] Trendyol adapter bulunamadi, atlaniyor", JobId);
                return;
            }

            var products = await adapter.PullProductsAsync(ct).ConfigureAwait(false);
            var pushed = 0;

            foreach (var product in products)
            {
                ct.ThrowIfCancellationRequested();
                var ok = await adapter.PushPriceUpdateAsync(product.Id, product.SalePrice, ct).ConfigureAwait(false);
                if (ok) pushed++;
            }

            _logger.LogInformation(
                "[{JobId}] Trendyol fiyat sync tamamlandi: {Pushed}/{Total} fiyat guncellendi",
                JobId, pushed, products.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[{JobId}] Trendyol fiyat sync iptal edildi", JobId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobId}] Trendyol fiyat sync HATA", JobId);
            throw;
        }
    }
}
