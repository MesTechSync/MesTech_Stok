using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 5 dakikada N11'den yeni siparisleri ceker.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class N11OrderSyncJob : ISyncJob
{
    public string JobId => "n11-order-sync";
    public string CronExpression => "*/5 * * * *";

    private readonly IAdapterFactory _factory;
    private readonly ILogger<N11OrderSyncJob> _logger;

    public N11OrderSyncJob(IAdapterFactory factory, ILogger<N11OrderSyncJob> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] N11 siparis sync basliyor...", JobId);

        try
        {
            var adapter = _factory.ResolveCapability<IOrderCapableAdapter>("N11");
            if (adapter == null)
            {
                _logger.LogWarning("[{JobId}] N11 IOrderCapableAdapter bulunamadi, atlaniyor", JobId);
                return;
            }

            var since = DateTime.UtcNow.AddHours(-1);
            var orders = await adapter.PullOrdersAsync(since, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[{JobId}] N11 siparis sync tamamlandi: {Count} siparis cekildi (son 1 saat)",
                JobId, orders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobId}] N11 siparis sync HATA", JobId);
            throw;
        }
    }
}
