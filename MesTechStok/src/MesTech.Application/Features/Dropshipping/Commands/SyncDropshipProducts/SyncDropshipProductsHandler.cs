#pragma warning disable MA0051 // Method is too long — sync handler is a single cohesive operation
#pragma warning disable NX0001 // NullForgiving justified — null check via IsNullOrWhiteSpace on line 45
using MediatR;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Dropshipping.Commands.SyncDropshipProducts;

/// <summary>
/// Tedarikçiden ürün senkronizasyonu yapar.
/// IDropshipFeedFetcher üzerinden feed çeker, ürünleri upsert eder.
/// K1d-05: Placeholder'dan gerçek implementasyona geçiş.
/// </summary>
public sealed class SyncDropshipProductsHandler : IRequestHandler<SyncDropshipProductsCommand, int>
{
    private readonly IDropshipSupplierRepository _supplierRepo;
    private readonly IDropshipProductRepository _productRepo;
    private readonly IUnitOfWork _uow;
    private readonly IDropshipFeedFetcher _feedFetcher;
    private readonly ILogger<SyncDropshipProductsHandler> _logger;

    public SyncDropshipProductsHandler(
        IDropshipSupplierRepository supplierRepo,
        IDropshipProductRepository productRepo,
        IUnitOfWork uow,
        IDropshipFeedFetcher feedFetcher,
        ILogger<SyncDropshipProductsHandler> logger)
    {
        _supplierRepo = supplierRepo;
        _productRepo = productRepo;
        _uow = uow;
        _feedFetcher = feedFetcher;
        _logger = logger;
    }

    public async Task<int> Handle(SyncDropshipProductsCommand request, CancellationToken cancellationToken)
    {
        var supplier = await _supplierRepo.GetByIdAsync(request.SupplierId, cancellationToken)
            ?? throw new InvalidOperationException($"Supplier not found: {request.SupplierId}");

        if (supplier.TenantId != request.TenantId)
            throw new InvalidOperationException("Supplier does not belong to the specified tenant.");

        // Tedarikçinin API endpoint'i yoksa sadece timestamp güncelle
        if (string.IsNullOrWhiteSpace(supplier.ApiEndpoint))
        {
            _logger.LogInformation(
                "SyncDropshipProducts — Supplier {SupplierId} has no API endpoint, recording sync only",
                request.SupplierId);
            supplier.RecordSync();
            await _supplierRepo.UpdateAsync(supplier, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
            return 0;
        }

        _logger.LogInformation(
            "SyncDropshipProducts — Fetching products from supplier {SupplierName} ({Endpoint})",
            supplier.Name, supplier.ApiEndpoint);

        // 1. Fetch feed from supplier API via infrastructure service
        var feedProducts = await _feedFetcher.FetchAsync(
            supplier.ApiEndpoint!, // NX0001: Null check is performed on line 45 above (IsNullOrWhiteSpace guard)
            supplier.ApiKey, cancellationToken);

        if (feedProducts.Count == 0)
        {
            _logger.LogWarning(
                "SyncDropshipProducts — No products returned from supplier {SupplierId}", request.SupplierId);
            supplier.RecordSync();
            await _supplierRepo.UpdateAsync(supplier, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
            return 0;
        }

        // 2. Get existing dropship products for this supplier
        var existingProducts = await _productRepo.GetBySupplierAsync(request.SupplierId, cancellationToken);
        var existingByExtId = existingProducts.ToDictionary(
            p => p.ExternalProductId, StringComparer.OrdinalIgnoreCase);

        // 3. Upsert: create new, update existing
        int syncedCount = 0;
        var toAdd = new List<DropshipProduct>();

        foreach (var feedItem in feedProducts)
        {
            if (string.IsNullOrWhiteSpace(feedItem.ExternalId) || string.IsNullOrWhiteSpace(feedItem.Title))
                continue;

            if (existingByExtId.TryGetValue(feedItem.ExternalId, out var existing))
            {
                // Update stock and price if changed
                bool changed = false;
                if (feedItem.Stock.HasValue && existing.StockQuantity != feedItem.Stock.Value)
                {
                    existing.UpdateStock(feedItem.Stock.Value);
                    changed = true;
                }
                if (feedItem.Price.HasValue && feedItem.Price.Value > 0
                    && existing.OriginalPrice != feedItem.Price.Value)
                {
                    existing.UpdatePrice(feedItem.Price.Value);
                    existing.ApplyMarkup(supplier.MarkupType, supplier.MarkupValue);
                    changed = true;
                }
                if (changed)
                {
                    await _productRepo.UpdateAsync(existing, cancellationToken);
                    syncedCount++;
                }
            }
            else
            {
                // Create new dropship product
                var newProduct = DropshipProduct.Create(
                    tenantId: request.TenantId,
                    supplierId: request.SupplierId,
                    externalProductId: feedItem.ExternalId,
                    title: feedItem.Title,
                    originalPrice: feedItem.Price ?? 0.01m,
                    stockQuantity: feedItem.Stock ?? 0);

                newProduct.ApplyMarkup(supplier.MarkupType, supplier.MarkupValue);
                toAdd.Add(newProduct);
                syncedCount++;
            }
        }

        if (toAdd.Count > 0)
            await _productRepo.AddRangeAsync(toAdd, cancellationToken);

        supplier.RecordSync();
        await _supplierRepo.UpdateAsync(supplier, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "SyncDropshipProducts — Synced {Count} products for supplier {SupplierName} " +
            "({Added} new, {Updated} updated)",
            syncedCount, supplier.Name, toAdd.Count, syncedCount - toAdd.Count);

        return syncedCount;
    }
}
