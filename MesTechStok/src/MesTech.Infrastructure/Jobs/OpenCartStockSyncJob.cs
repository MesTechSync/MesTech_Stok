using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 30 dakikada OpenCart ile cift yonlu stok sync yapar.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class OpenCartStockSyncJob : ISyncJob
{
    public string JobId => "opencart-stock-sync";
    public string CronExpression => "*/30 * * * *"; // Her 30 dk

    private readonly IAdapterFactory _factory;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<OpenCartStockSyncJob> _logger;

    public OpenCartStockSyncJob(IAdapterFactory factory, ITenantProvider tenantProvider, ILogger<OpenCartStockSyncJob> logger)
    {
        _factory = factory;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        _logger.LogInformation("[{JobId}] OpenCart stok sync basliyor... TenantId={TenantId}", JobId, tenantId);

        try
        {
            var adapter = _factory.Resolve("OpenCart");
            if (adapter == null)
            {
                _logger.LogWarning("[{JobId}] OpenCart adapter bulunamadi, atlaniyor", JobId);
                return;
            }

            // Pull current products from OpenCart
            var products = await adapter.PullProductsAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("[{JobId}] OpenCart'tan {Count} urun cekildi", JobId, products.Count);

            // Push stock updates for reconciliation
            var pushed = 0;
            foreach (var product in products)
            {
                ct.ThrowIfCancellationRequested();
                var ok = await adapter.PushStockUpdateAsync(product.Id, product.Stock, ct).ConfigureAwait(false);
                if (ok) pushed++;
            }

            _logger.LogInformation(
                "[{JobId}] OpenCart stok sync tamamlandi: {Pushed}/{Total} urun guncellendi",
                JobId, pushed, products.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[{JobId}] OpenCart stok sync iptal edildi", JobId);
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[{JobId}] OpenCart stok sync HATA", JobId);
            throw;
        }
    }
}
