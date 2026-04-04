using MediatR;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Commands.SyncSupplierPrices;

/// <summary>
/// Tedarikçi feed'inden fiyatları çeker ve DropshipProduct.OriginalPrice'ı günceller.
/// Linked ürünlerde Product.PurchasePrice da güncellenir.
/// </summary>
public sealed class SyncSupplierPricesHandler : IRequestHandler<SyncSupplierPricesCommand, PriceSyncResultDto>
{
    private readonly IDropshipSupplierRepository _supplierRepository;
    private readonly IDropshipProductRepository _productRepository;
    private readonly IProductRepository _mainProductRepository;
    private readonly IDropshipFeedFetcher _feedFetcher;
    private readonly IUnitOfWork _unitOfWork;

    public SyncSupplierPricesHandler(
        IDropshipSupplierRepository supplierRepository,
        IDropshipProductRepository productRepository,
        IProductRepository mainProductRepository,
        IDropshipFeedFetcher feedFetcher,
        IUnitOfWork unitOfWork)
    {
        _supplierRepository = supplierRepository;
        _productRepository = productRepository;
        _mainProductRepository = mainProductRepository;
        _feedFetcher = feedFetcher;
        _unitOfWork = unitOfWork;
    }

    // MA0051: Handler orchestrates full price sync pipeline — splitting would reduce readability
#pragma warning disable MA0051 // Method is too long
    public async Task<PriceSyncResultDto> Handle(
        SyncSupplierPricesCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId, cancellationToken)
            ?? throw new InvalidOperationException($"Supplier '{request.SupplierId}' not found.");

        if (string.IsNullOrWhiteSpace(supplier.ApiEndpoint))
            throw new InvalidOperationException($"Supplier '{supplier.Name}' has no API endpoint configured.");

        // Feed'den fiyat verilerini çek
        var feedItems = await _feedFetcher.FetchAsync(
            supplier.ApiEndpoint, supplier.ApiKey, cancellationToken);

        // Tedarikçiye ait tüm ürünleri al
        var existingProducts = await _productRepository.GetBySupplierAsync(
            supplier.Id, cancellationToken);

        var productLookup = existingProducts.ToDictionary(
            p => p.ExternalProductId, StringComparer.OrdinalIgnoreCase);

        int updated = 0;
        int unchanged = 0;
        var errors = new List<PriceSyncErrorDto>();

        foreach (var feedItem in feedItems)
        {
            if (string.IsNullOrWhiteSpace(feedItem.ExternalId))
            {
                errors.Add(new PriceSyncErrorDto
                {
                    ExternalProductId = feedItem.ExternalId ?? "(null)",
                    Reason = "External ID is missing"
                });
                continue;
            }

            if (!feedItem.Price.HasValue || feedItem.Price.Value < 0)
            {
                errors.Add(new PriceSyncErrorDto
                {
                    ExternalProductId = feedItem.ExternalId,
                    Reason = $"Invalid price value: {feedItem.Price}"
                });
                continue;
            }

            if (!productLookup.TryGetValue(feedItem.ExternalId, out var dropshipProduct))
            {
                // Ürün henüz sisteme eklenmemiş — atla
                continue;
            }

            // Fiyat değişmemiş mi kontrol et
            if (dropshipProduct.OriginalPrice == feedItem.Price.Value)
            {
                unchanged++;
                continue;
            }

            // DropshipProduct fiyatını güncelle
            dropshipProduct.UpdatePrice(feedItem.Price.Value);
            dropshipProduct.ApplyMarkup(supplier.MarkupType, supplier.MarkupValue);
            await _productRepository.UpdateAsync(dropshipProduct, cancellationToken).ConfigureAwait(false);

            // Linked ürünse ana Product.PurchasePrice'ı da güncelle
            if (dropshipProduct.IsLinked && dropshipProduct.ProductId.HasValue)
            {
                var mainProduct = await _mainProductRepository.GetByIdAsync(dropshipProduct.ProductId.Value, cancellationToken).ConfigureAwait(false);
                if (mainProduct is not null)
                {
                    mainProduct.PurchasePrice = feedItem.Price.Value;
                    mainProduct.UpdatedAt = DateTime.UtcNow;
                    await _mainProductRepository.UpdateAsync(mainProduct, cancellationToken).ConfigureAwait(false);
                }
            }

            updated++;
        }

        if (updated > 0)
        {
            supplier.RecordSync();
            await _supplierRepository.UpdateAsync(supplier, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new PriceSyncResultDto
        {
            Updated = updated,
            Unchanged = unchanged,
            Errors = errors.Count,
            ErrorDetails = errors
        };
    }
#pragma warning restore MA0051
}
