using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// ERP fiyat sync job — gunluk 4 kez calisir (06:00, 12:00, 18:00, 23:00).
/// ERP'den urun fiyatlarini ceker ve MesTech'e gunceller.
/// Phase-2 TAM: IErpPriceCapable adapter'lardan fiyat ceker, SKU eslestirip Product.UpdatePrice cagirir.
/// </summary>
[AutomaticRetry(Attempts = 3)]
public sealed class ErpPriceSyncJob : ISyncJob
{
    public string JobId => "erp-price-sync";
    public string CronExpression => "0 6,12,18,23 * * *"; // Gunluk 4x

    private readonly IErpAdapterFactory _factory;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ErpPriceSyncJob> _logger;

    public ErpPriceSyncJob(
        IErpAdapterFactory factory,
        IProductRepository productRepository,
        ILogger<ErpPriceSyncJob> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] ERP fiyat sync basliyor...", JobId);

        var providers = _factory.SupportedProviders;
        var totalUpdated = 0;
        var totalSkipped = 0;
        var totalFailed = 0;

        foreach (var provider in providers)
        {
            ct.ThrowIfCancellationRequested();

#pragma warning disable CA1031 // Intentional: per-adapter isolation — one failure must not stop others
            try
            {
                var adapter = _factory.GetAdapter(provider);

                if (adapter is not IErpPriceCapable priceCapable)
                {
                    _logger.LogDebug(
                        "[{JobId}] {Provider} does not implement IErpPriceCapable, skipping",
                        JobId, provider);
                    continue;
                }

                var isAlive = await adapter.PingAsync(ct).ConfigureAwait(false);
                if (!isAlive)
                {
                    _logger.LogWarning(
                        "[{JobId}] {Provider} ping failed, skipping price sync", JobId, provider);
                    continue;
                }

                var erpPrices = await priceCapable.GetProductPricesAsync(ct).ConfigureAwait(false);
                if (erpPrices.Count == 0)
                {
                    _logger.LogInformation(
                        "[{JobId}] {Provider} returned 0 price items", JobId, provider);
                    continue;
                }

                foreach (var priceItem in erpPrices)
                {
                    ct.ThrowIfCancellationRequested();

                    var product = await _productRepository.GetBySKUAsync(priceItem.ProductCode).ConfigureAwait(false);
                    if (product is null)
                    {
                        totalSkipped++;
                        continue;
                    }

                    if (product.SalePrice == priceItem.SalePrice && product.PurchasePrice == priceItem.PurchasePrice)
                    {
                        totalSkipped++;
                        continue;
                    }

                    product.PurchasePrice = priceItem.PurchasePrice;
                    product.UpdatePrice(priceItem.SalePrice);

                    if (priceItem.ListPrice.HasValue)
                        product.ListPrice = priceItem.ListPrice.Value;

                    await _productRepository.UpdateAsync(product).ConfigureAwait(false);
                    totalUpdated++;
                }

                _logger.LogInformation(
                    "[{JobId}] {Provider} price sync: {Count} items fetched, {Updated} updated, {Skipped} skipped",
                    JobId, provider, erpPrices.Count, totalUpdated, totalSkipped);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[{JobId}] ERP fiyat sync iptal edildi ({Provider})", JobId, provider);
                throw;
            }
            catch (Exception ex)
            {
                totalFailed++;
                _logger.LogError(ex,
                    "[{JobId}] {Provider} fiyat sync HATA", JobId, provider);
            }
#pragma warning restore CA1031
        }

        _logger.LogInformation(
            "[{JobId}] ERP fiyat sync tamamlandi: {Updated} updated, {Skipped} skipped, {Failed} failed",
            JobId, totalUpdated, totalSkipped, totalFailed);
    }
}
