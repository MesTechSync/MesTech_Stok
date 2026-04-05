using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 5 dakikada Ciceksepeti'nden yeni siparisleri ceker.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class CiceksepetiOrderSyncJob : ISyncJob
{
    public string JobId => "ciceksepeti-order-sync";
    public string CronExpression => "*/5 * * * *";

    private readonly IAdapterFactory _factory;
    private readonly ILogger<CiceksepetiOrderSyncJob> _logger;

    public CiceksepetiOrderSyncJob(IAdapterFactory factory, ILogger<CiceksepetiOrderSyncJob> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Ciceksepeti siparis sync basliyor...", JobId);

        try
        {
            var adapter = _factory.ResolveCapability<IOrderCapableAdapter>("Ciceksepeti");
            if (adapter == null)
            {
                _logger.LogWarning("[{JobId}] Ciceksepeti IOrderCapableAdapter bulunamadi, atlaniyor", JobId);
                return;
            }

            var since = DateTime.UtcNow.AddHours(-1);
            var orders = await adapter.PullOrdersAsync(since, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[{JobId}] Ciceksepeti siparis sync tamamlandi: {Count} siparis cekildi (son 1 saat)",
                JobId, orders.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[{JobId}] Ciceksepeti siparis sync HATA", JobId);
            throw;
        }
    }
}
