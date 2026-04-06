using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 5 dakikada Hepsiburada'dan yeni siparisleri ceker.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class HepsiburadaOrderSyncJob : ISyncJob
{
    public string JobId => "hepsiburada-order-sync";
    public string CronExpression => "*/5 * * * *";

    private readonly IAdapterFactory _factory;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<HepsiburadaOrderSyncJob> _logger;

    public HepsiburadaOrderSyncJob(IAdapterFactory factory, ITenantProvider tenantProvider, ILogger<HepsiburadaOrderSyncJob> logger)
    {
        _factory = factory;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        _logger.LogInformation("[{JobId}] Hepsiburada siparis sync basliyor... TenantId={TenantId}", JobId, tenantId);

        try
        {
            var adapter = _factory.ResolveCapability<IOrderCapableAdapter>("Hepsiburada");
            if (adapter == null)
            {
                _logger.LogWarning("[{JobId}] Hepsiburada IOrderCapableAdapter bulunamadi, atlaniyor", JobId);
                return;
            }

            var since = DateTime.UtcNow.AddHours(-1);
            var orders = await adapter.PullOrdersAsync(since, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[{JobId}] Hepsiburada siparis sync tamamlandi: {Count} siparis cekildi (son 1 saat)",
                JobId, orders.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[{JobId}] Hepsiburada siparis sync HATA", JobId);
            throw;
        }
    }
}
