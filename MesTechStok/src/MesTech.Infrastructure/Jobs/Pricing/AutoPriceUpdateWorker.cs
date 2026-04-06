using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs.Pricing;

/// <summary>
/// Otomatik fiyat güncelleme worker — Sentos rekabet analizi karşılığı.
/// Her 30 dakikada bir çalışır:
///   1. Tüm tenant'lar için buybox kaybedilen ürünleri tespit eder
///   2. IPriceOptimizationService ile optimal fiyat hesaplar
///   3. Min/max aralığında ise platform fiyatını otomatik günceller
///   4. Güncelleme log'u tutar (audit trail)
///
/// Rakip keşif: Sentos 30dk'da bir otomatik fiyat güncelleme yapıyor.
/// Bu worker MesTech'in aynı yeteneğini sağlar.
/// </summary>
[AutomaticRetry(Attempts = 2)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class AutoPriceUpdateWorker
{
    public static string JobId => "pricing-auto-update";
    public static string CronExpression => "*/30 * * * *"; // Her 30 dakika

    private readonly IBuyboxService _buyboxService;
    private readonly IPriceOptimizationService _priceOptimizationService;
    private readonly IProductRepository _productRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDashboardNotifier _notifier;
    private readonly ILogger<AutoPriceUpdateWorker> _logger;

    public AutoPriceUpdateWorker(
        IBuyboxService buyboxService,
        IPriceOptimizationService priceOptimizationService,
        IProductRepository productRepository,
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        IDashboardNotifier notifier,
        ILogger<AutoPriceUpdateWorker> logger)
    {
        _buyboxService = buyboxService;
        _priceOptimizationService = priceOptimizationService;
        _productRepository = productRepository;
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
        _logger = logger;
    }

    [JobDisplayName("Auto Price Update — Buybox Recovery")]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[AutoPrice] Starting automatic price update cycle");

        var allTenants = await _tenantRepository.GetAllAsync(ct).ConfigureAwait(false);
        var tenants = allTenants.Where(t => t.IsActive).ToList();
        var totalUpdated = 0;
        var totalSkipped = 0;

        foreach (var tenant in tenants)
        {
            try
            {
                var (updated, skipped) = await ProcessTenantAsync(tenant.Id, ct).ConfigureAwait(false);
                totalUpdated += updated;
                totalSkipped += skipped;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex,
                    "[AutoPrice] Tenant {TenantId} işlenirken hata — devam ediliyor", tenant.Id);
            }
        }

        _logger.LogInformation(
            "[AutoPrice] Cycle completed — {Updated} updated, {Skipped} skipped, {TenantCount} tenants",
            totalUpdated, totalSkipped, tenants.Count);

        // Broadcast cycle completion to all tenants
        try
        {
            await _notifier.NotifyPriceCycleDoneAsync(
                Domain.Constants.DomainConstants.SystemTenantId, totalUpdated, totalSkipped, tenants.Count, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "[AutoPrice] Cycle-done notification failed");
        }
    }

    private async Task<(int Updated, int Skipped)> ProcessTenantAsync(Guid tenantId, CancellationToken ct)
    {
        // Buybox kaybedilen ürünleri al
        var lostBuyboxes = await _buyboxService.GetLostBuyboxesAsync(tenantId, ct).ConfigureAwait(false);

        if (lostBuyboxes.Count == 0)
            return (0, 0);

        _logger.LogInformation(
            "[AutoPrice] Tenant {TenantId}: {Count} lost buybox found",
            tenantId, lostBuyboxes.Count);

        var updated = 0;
        var skipped = 0;

        foreach (var lost in lostBuyboxes)
        {
            var product = await _productRepository.GetByIdAsync(lost.ProductId, ct).ConfigureAwait(false);
            if (product is null)
            {
                skipped++;
                continue;
            }

            // AI fiyat optimizasyonu
            var optimization = await _priceOptimizationService.OptimizePriceAsync(
                product.Id, product.SalePrice, product.PurchasePrice,
                "trendyol", ct).ConfigureAwait(false);

            // Min/max güvenlik kontrolü
            var minPrice = product.PurchasePrice * 1.05m; // Minimum %5 marj
            var maxPrice = product.SalePrice * 1.20m;      // Mevcut fiyatın %20 üstü max

            if (optimization.RecommendedPrice < minPrice)
            {
                _logger.LogWarning(
                    "[AutoPrice] {SKU}: Önerilen fiyat {Recommended} < min {Min} — ATLA",
                    product.SKU, optimization.RecommendedPrice, minPrice);
                skipped++;
                continue;
            }

            if (optimization.RecommendedPrice > maxPrice)
            {
                _logger.LogWarning(
                    "[AutoPrice] {SKU}: Önerilen fiyat {Recommended} > max {Max} — ATLA",
                    product.SKU, optimization.RecommendedPrice, maxPrice);
                skipped++;
                continue;
            }

            // Fiyat farkı %1'den az ise güncelleme yapma (gereksiz API çağrısı önle)
            var priceDiffPct = Math.Abs(product.SalePrice - optimization.RecommendedPrice) / product.SalePrice;
            if (priceDiffPct < 0.01m)
            {
                skipped++;
                continue;
            }

            // Fiyat güncelle
            product.UpdatePrice(optimization.RecommendedPrice);
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[AutoPrice] {SKU}: {OldPrice} → {NewPrice} ({Strategy})",
                product.SKU, lost.CurrentPrice, optimization.RecommendedPrice, optimization.Strategy);

            // Real-time notification — fiyat değişikliğini dashboard'a push et
            try
            {
                await _notifier.NotifyPriceAutoUpdatedAsync(
                    tenantId, product.SKU, lost.CurrentPrice,
                    optimization.RecommendedPrice, optimization.Strategy.ToString(), ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "[AutoPrice] SignalR notification failed for {SKU} — pricing continues", product.SKU);
            }

            updated++;
        }

        return (updated, skipped);
    }
}
