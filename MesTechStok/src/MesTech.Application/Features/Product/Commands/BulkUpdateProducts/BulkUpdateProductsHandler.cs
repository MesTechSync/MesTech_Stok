using System.Globalization;
using MediatR;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Product.Commands.BulkUpdateProducts;

/// <summary>
/// Toplu ürün güncelleme handler'ı.
/// Batch processing ile seçili ürünlere aksiyon uygular.
/// </summary>
public sealed class BulkUpdateProductsHandler : IRequestHandler<BulkUpdateProductsCommand, int>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<BulkUpdateProductsHandler> _logger;

    public BulkUpdateProductsHandler(
        IProductRepository productRepository,
        IUnitOfWork uow,
        ILogger<BulkUpdateProductsHandler> logger)
    {
        _productRepository = productRepository;
        _uow = uow;
        _logger = logger;
    }

    public async Task<int> Handle(
        BulkUpdateProductsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ProductIds is null || request.ProductIds.Count == 0)
            return 0;

        var updatedCount = 0;

        foreach (var productId in request.ProductIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var product = await _productRepository.GetByIdAsync(productId);
            if (product is null)
            {
                _logger.LogWarning("Ürün bulunamadı: {ProductId}", productId);
                continue;
            }

            try
            {
                ApplyAction(product, request.Action, request.Value);
                product.UpdatedAt = DateTime.UtcNow;
                await _productRepository.UpdateAsync(product);
                updatedCount++;
            }
#pragma warning disable CA1031 // Catch general exception — batch processing must not abort on single item failure
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(ex, "Toplu güncelleme hatası — Ürün: {ProductId}, Aksiyon: {Action}",
                    productId, request.Action);
            }
        }

        if (updatedCount > 0)
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Toplu güncelleme tamamlandı: {Updated}/{Total} ürün, Aksiyon: {Action}",
            updatedCount, request.ProductIds.Count, request.Action);

        return updatedCount;
    }

    private static void ApplyAction(
        Domain.Entities.Product product, BulkUpdateAction action, object? value)
    {
        switch (action)
        {
            case BulkUpdateAction.PriceIncreasePercent:
                var increasePercent = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                product.UpdatePrice(Math.Round(product.SalePrice * (1 + increasePercent / 100m), 2));
                break;

            case BulkUpdateAction.PriceDecreasePercent:
                var decreasePercent = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                product.UpdatePrice(Math.Round(product.SalePrice * (1 - decreasePercent / 100m), 2));
                break;

            case BulkUpdateAction.PriceSetFixed:
                product.UpdatePrice(Convert.ToDecimal(value, CultureInfo.InvariantCulture));
                break;

            case BulkUpdateAction.StockSet:
                var newStock = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                var setDelta = newStock - product.Stock;
                if (setDelta != 0)
                    product.AdjustStock(setDelta, Domain.Enums.StockMovementType.Adjustment, "Bulk stock set");
                break;

            case BulkUpdateAction.StockAdd:
                var addQty = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                if (addQty != 0)
                    product.AdjustStock(addQty, Domain.Enums.StockMovementType.Adjustment, "Bulk stock add");
                break;

            case BulkUpdateAction.StatusActivate:
                product.Activate();
                break;

            case BulkUpdateAction.StatusDeactivate:
                product.Deactivate();
                break;

            case BulkUpdateAction.CategoryAssign:
                product.CategoryId = Guid.Parse(value?.ToString() ?? throw new ArgumentNullException(nameof(value)));
                break;

            case BulkUpdateAction.PlatformPublish:
            case BulkUpdateAction.PlatformUnpublish:
                // Platform publish/unpublish requires platform adapter integration.
                // Handled at higher orchestration layer — mark intent here.
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, "Desteklenmeyen toplu güncelleme aksiyonu.");
        }
    }
}
