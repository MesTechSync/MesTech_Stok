using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Tüm platformlar için periyodik ürün import (pull) + DB persist.
/// Platform kodu parametre olarak alınır.
/// Akis: PullProductsAsync → barcode duplicate check → AddAsync/Update → SaveChanges.
/// NOT: Bu job platformdan DB'ye ürün IMPORT eder (tek yönlü).
/// Stok PUSH: GenericPlatformStockSyncJob (ayrı job).
/// </summary>
[AutomaticRetry(Attempts = 2)]
[DisableConcurrentExecution(timeoutInSeconds: 600)]
public sealed class GenericPlatformProductSyncJob
{
    private readonly IAdapterFactory _factory;
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<GenericPlatformProductSyncJob> _logger;

    public GenericPlatformProductSyncJob(
        IAdapterFactory factory,
        IProductRepository productRepo,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        ILogger<GenericPlatformProductSyncJob> logger)
    {
        _factory = factory;
        _productRepo = productRepo;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(string platformCode, CancellationToken ct = default)
    {
        _logger.LogInformation("[ProductSync] {Platform} ürün import başlıyor...", platformCode);

        var adapter = _factory.Resolve(platformCode);
        if (adapter is null)
        {
            _logger.LogWarning("[ProductSync] {Platform} adapter bulunamadı — atlaniyor", platformCode);
            return;
        }

        try
        {
            var products = await adapter.PullProductsAsync(ct).ConfigureAwait(false);

            if (products.Count == 0)
            {
                _logger.LogDebug("[ProductSync] {Platform} ürün yok", platformCode);
                return;
            }

            var tenantId = _tenantProvider.GetCurrentTenantId();
            int created = 0, updated = 0, skipped = 0;

            foreach (var product in products)
            {
                if (ct.IsCancellationRequested) break;

                // Barcode ile duplicate check
                Product? existing = null;
                if (!string.IsNullOrEmpty(product.Barcode))
                    existing = await _productRepo.GetByBarcodeAsync(product.Barcode, ct).ConfigureAwait(false);

                if (existing is not null)
                {
                    // Stok ve fiyat güncelle (mevcut ürün)
                    var changed = false;
                    if (existing.Stock != product.Stock) { existing.SyncStock(product.Stock, "platform-sync"); changed = true; }
                    if (existing.SalePrice != product.SalePrice) { existing.SalePrice = product.SalePrice; changed = true; }
                    if (product.ListPrice.HasValue && existing.ListPrice != product.ListPrice) { existing.ListPrice = product.ListPrice; changed = true; }

                    if (changed) { await _productRepo.UpdateAsync(existing, ct).ConfigureAwait(false); updated++; }
                    else skipped++;
                    continue;
                }

                // Yeni ürün — TenantId set et
                product.TenantId = tenantId;
                if (product.Id == Guid.Empty) product.Id = Guid.NewGuid();

                await _productRepo.AddAsync(product, ct).ConfigureAwait(false);
                created++;

                // Her 100 üründe batch save (memory pressure azaltma)
                if ((created + updated) % 100 == 0)
                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            if (created > 0 || updated > 0)
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[ProductSync] {Platform} TAMAMLANDI — {Total} çekildi, {Created} oluşturuldu, {Updated} güncellendi, {Skipped} değişmemiş",
                platformCode, products.Count, created, updated, skipped);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[ProductSync] {Platform} ürün import BAŞARISIZ", platformCode);
            throw;
        }
    }
}
