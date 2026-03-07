using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 5 dakikada Trendyol'dan yeni siparisleri ceker.
/// </summary>
public class TrendyolOrderSyncJob : ISyncJob
{
    public string JobId => "trendyol-order-sync";
    public string CronExpression => "*/5 * * * *"; // Her 5 dk

    private readonly ILogger<TrendyolOrderSyncJob> _logger;

    public TrendyolOrderSyncJob(ILogger<TrendyolOrderSyncJob> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Trendyol siparis sync basliyor...", JobId);

        // TODO: TrendyolAdapter.PullOrdersAsync() cagirilacak
        // Siparis gelince: OrderReceivedEvent -> stok dusur -> fatura olustur

        await Task.CompletedTask;
        _logger.LogInformation("[{JobId}] Trendyol siparis sync tamamlandi", JobId);
    }
}
