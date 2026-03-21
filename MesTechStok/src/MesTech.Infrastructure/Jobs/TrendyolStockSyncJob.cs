using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 30 dakikada stok delta sync Trendyol'a push eder.
/// </summary>
[AutomaticRetry(Attempts = 3)]
public class TrendyolStockSyncJob : ISyncJob
{
    public string JobId => "trendyol-stock-sync";
    public string CronExpression => "*/30 * * * *"; // Her 30 dk

    private readonly IAdapterFactory _factory;
    private readonly ILogger<TrendyolStockSyncJob> _logger;

    public TrendyolStockSyncJob(IAdapterFactory factory, ILogger<TrendyolStockSyncJob> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Trendyol stok sync basliyor...", JobId);

        try
        {
            var adapter = _factory.Resolve("Trendyol");
            if (adapter == null)
            {
                _logger.LogWarning("[{JobId}] Trendyol adapter bulunamadi, atlaniyor", JobId);
                return;
            }

            // Pull current product list, then push stock for each
            var products = await adapter.PullProductsAsync(ct);
            var pushed = 0;

            foreach (var product in products)
            {
                ct.ThrowIfCancellationRequested();
                var ok = await adapter.PushStockUpdateAsync(product.Id, product.Stock, ct);
                if (ok) pushed++;
            }

            _logger.LogInformation(
                "[{JobId}] Trendyol stok sync tamamlandi: {Pushed}/{Total} urun guncellendi",
                JobId, pushed, products.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[{JobId}] Trendyol stok sync iptal edildi", JobId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobId}] Trendyol stok sync HATA", JobId);
            throw;
        }
    }
}
