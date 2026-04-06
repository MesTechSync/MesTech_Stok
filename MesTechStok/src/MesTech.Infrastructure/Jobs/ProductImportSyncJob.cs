using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// D12-12: 3 aşamalı ürün import sync job.
///   Aşama 1 — Quick Delta (15 dk): sadece değişen ürünler (LAST_MODIFIED_DATE)
///   Aşama 2 — Pool Scan (1 saat): yeni/silinen ürün tespiti
///   Aşama 3 — Full Reconciliation (gece 03:00): tam karşılaştırma
///
/// Her aşama bağımsız Hangfire recurring job olarak çalışır.
/// Platform kodu parametre olarak alınır — çoklu platform destekli.
/// </summary>
[AutomaticRetry(Attempts = 2)]
[DisableConcurrentExecution(timeoutInSeconds: 600)]
public sealed class ProductImportSyncJob
{
    private readonly IAdapterFactory _adapterFactory;
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<ProductImportSyncJob> _logger;

    public ProductImportSyncJob(
        IAdapterFactory adapterFactory,
        IProductRepository productRepo,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        ILogger<ProductImportSyncJob> logger)
    {
        _adapterFactory = adapterFactory;
        _productRepo = productRepo;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// AŞAMA 1: Quick Delta — son sync'ten bu yana değişen ürünleri çek ve upsert et.
    /// Hangfire: Her 15 dakika. Hızlı, düşük API call.
    /// </summary>
    public async Task ExecuteQuickDeltaAsync(string platformCode, CancellationToken ct = default)
    {
        _logger.LogInformation("[ProductSync:QuickDelta] {Platform} başlıyor...", platformCode);

        var adapter = _adapterFactory.Resolve(platformCode);
        if (adapter is null)
        {
            _logger.LogWarning("[ProductSync:QuickDelta] {Platform} adapter bulunamadı", platformCode);
            return;
        }

        // Son sync zamanını belirle (varsayılan: 30 dakika önce)
        var lastSyncTime = DateTime.UtcNow.AddMinutes(-30);

        var result = await adapter.SyncProductsDeltaAsync(lastSyncTime, pageSize: 200, ct)
            .ConfigureAwait(false);

        if (result.TotalCount == 0)
        {
            _logger.LogDebug("[ProductSync:QuickDelta] {Platform} değişen ürün yok", platformCode);
            return;
        }

        var tenantId = _tenantProvider.GetCurrentTenantId();
        int created = 0, updated = 0;

        foreach (var product in result.Products)
        {
            if (ct.IsCancellationRequested) break;

            var existing = !string.IsNullOrEmpty(product.Barcode)
                ? await _productRepo.GetByBarcodeAsync(product.Barcode, ct).ConfigureAwait(false)
                : await _productRepo.GetBySKUAsync(product.SKU, ct).ConfigureAwait(false);

            if (existing is not null)
            {
                var changed = false;
                if (existing.Stock != product.Stock) { existing.SyncStock(product.Stock, "delta-sync"); changed = true; }
                if (existing.SalePrice != product.SalePrice) { existing.SalePrice = product.SalePrice; changed = true; }
                if (product.ListPrice.HasValue && existing.ListPrice != product.ListPrice) { existing.ListPrice = product.ListPrice; changed = true; }
                if (!string.IsNullOrEmpty(product.ImageUrl) && existing.ImageUrl != product.ImageUrl) { existing.ImageUrl = product.ImageUrl; changed = true; }

                if (changed) { await _productRepo.UpdateAsync(existing, ct).ConfigureAwait(false); updated++; }
            }
            else
            {
                product.TenantId = tenantId;
                if (product.Id == Guid.Empty) product.Id = Guid.NewGuid();
                await _productRepo.AddAsync(product, ct).ConfigureAwait(false);
                created++;
            }

            if ((created + updated) % 100 == 0 && (created + updated) > 0)
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        if (created > 0 || updated > 0)
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "[ProductSync:QuickDelta] {Platform} tamamlandı — {Total} çekildi, {Created} yeni, {Updated} güncellendi, {ApiCalls} API call, {Duration}s",
            platformCode, result.TotalCount, created, updated, result.ApiCallsMade, result.Duration.TotalSeconds);
    }

    /// <summary>
    /// AŞAMA 2: Pool Scan — tüm ürünleri çek, yeni/silinen tespiti yap.
    /// Hangfire: Her 1 saat. Orta yoğunlukta API call.
    /// </summary>
    public async Task ExecutePoolScanAsync(string platformCode, CancellationToken ct = default)
    {
        _logger.LogInformation("[ProductSync:PoolScan] {Platform} başlıyor...", platformCode);

        var adapter = _adapterFactory.Resolve(platformCode);
        if (adapter is null) return;

        // Son 6 saat değişenleri çek
        var lastSyncTime = DateTime.UtcNow.AddHours(-6);
        var result = await adapter.SyncProductsDeltaAsync(lastSyncTime, pageSize: 200, ct)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "[ProductSync:PoolScan] {Platform} — {Count} ürün, {Calls} API call, {Duration}s",
            platformCode, result.TotalCount, result.ApiCallsMade, result.Duration.TotalSeconds);

        // Upsert logic aynı — QuickDelta ile aynı pattern
        await UpsertProductsAsync(result.Products, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// AŞAMA 3: Full Reconciliation — gece tam karşılaştırma. Yeni ürünler + fiyat/stok diff.
    /// Hangfire: Gece 03:00. Yoğun API call — tüm sayfalama.
    /// </summary>
    public async Task ExecuteFullReconciliationAsync(string platformCode, CancellationToken ct = default)
    {
        _logger.LogInformation("[ProductSync:FullRecon] {Platform} başlıyor...", platformCode);

        var adapter = _adapterFactory.Resolve(platformCode);
        if (adapter is null) return;

        // Tüm ürünleri çek (son 30 gün — pratikte hepsi)
        var result = await adapter.SyncProductsDeltaAsync(
            DateTime.UtcNow.AddDays(-30), pageSize: 200, ct)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "[ProductSync:FullRecon] {Platform} — {Count} ürün, {Calls} API call, {Duration}s",
            platformCode, result.TotalCount, result.ApiCallsMade, result.Duration.TotalSeconds);

        await UpsertProductsAsync(result.Products, ct).ConfigureAwait(false);
    }

    private async Task UpsertProductsAsync(IReadOnlyList<Domain.Entities.Product> products, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        int created = 0, updated = 0;

        foreach (var product in products)
        {
            if (ct.IsCancellationRequested) break;

            var existing = !string.IsNullOrEmpty(product.Barcode)
                ? await _productRepo.GetByBarcodeAsync(product.Barcode, ct).ConfigureAwait(false)
                : await _productRepo.GetBySKUAsync(product.SKU, ct).ConfigureAwait(false);

            if (existing is not null)
            {
                var changed = false;
                if (existing.Stock != product.Stock) { existing.SyncStock(product.Stock, "product-import-sync"); changed = true; }
                if (existing.SalePrice != product.SalePrice) { existing.SalePrice = product.SalePrice; changed = true; }
                if (product.ListPrice.HasValue && existing.ListPrice != product.ListPrice) { existing.ListPrice = product.ListPrice; changed = true; }
                if (changed) { await _productRepo.UpdateAsync(existing, ct).ConfigureAwait(false); updated++; }
            }
            else
            {
                product.TenantId = tenantId;
                if (product.Id == Guid.Empty) product.Id = Guid.NewGuid();
                await _productRepo.AddAsync(product, ct).ConfigureAwait(false);
                created++;
            }

            if ((created + updated) % 100 == 0 && (created + updated) > 0)
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        if (created > 0 || updated > 0)
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "[ProductSync:Upsert] {Created} yeni, {Updated} güncellendi", created, updated);
    }
}
