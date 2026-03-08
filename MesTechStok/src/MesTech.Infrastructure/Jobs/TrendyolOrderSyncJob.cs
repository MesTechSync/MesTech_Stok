using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 5 dakikada Trendyol'dan yeni siparisleri ceker.
/// </summary>
public class TrendyolOrderSyncJob : ISyncJob
{
    public string JobId => "trendyol-order-sync";
    public string CronExpression => "*/5 * * * *"; // Her 5 dk

    private readonly IAdapterFactory _factory;
    private readonly ILogger<TrendyolOrderSyncJob> _logger;

    public TrendyolOrderSyncJob(IAdapterFactory factory, ILogger<TrendyolOrderSyncJob> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Trendyol siparis sync basliyor...", JobId);

        try
        {
            var adapter = _factory.ResolveCapability<IOrderCapableAdapter>("Trendyol");
            if (adapter == null)
            {
                _logger.LogWarning("[{JobId}] Trendyol IOrderCapableAdapter bulunamadi, atlaniyor", JobId);
                return;
            }

            var since = DateTime.UtcNow.AddHours(-1);
            var orders = await adapter.PullOrdersAsync(since, ct);

            _logger.LogInformation(
                "[{JobId}] Trendyol siparis sync tamamlandi: {Count} siparis cekildi (son 1 saat)",
                JobId, orders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobId}] Trendyol siparis sync HATA", JobId);
            throw; // Hangfire retry mekanizmasi devralir
        }
    }
}
