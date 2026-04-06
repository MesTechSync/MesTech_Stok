using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 6 saatte Trendyol ile fiyat esitlemesi yapar.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class TrendyolPriceSyncJob : ISyncJob
{
    public string JobId => "trendyol-price-sync";
    public string CronExpression => "0 */6 * * *"; // Her 6 saat

    private readonly IAdapterFactory _factory;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TrendyolPriceSyncJob> _logger;

    public TrendyolPriceSyncJob(IAdapterFactory factory, ITenantProvider tenantProvider, ILogger<TrendyolPriceSyncJob> logger)
    {
        _factory = factory;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        _logger.LogInformation("[{JobId}] Trendyol fiyat sync basliyor... TenantId={TenantId}", JobId, tenantId);

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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[{JobId}] Trendyol fiyat sync HATA", JobId);
            throw;
        }
    }
}
