using MediatR;
using MesTech.Application.DTOs;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.BulkUpdateStock;

public sealed class BulkUpdateStockHandler : IRequestHandler<BulkUpdateStockCommand, BulkUpdateResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BulkUpdateStockHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<BulkUpdateResult> Handle(BulkUpdateStockCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var failures = new List<BulkUpdateFailure>();
        int successCount = 0;

        foreach (var item in request.Items)
        {
            if (item.NewStock < 0)
            {
                failures.Add(new BulkUpdateFailure
                {
                    Sku = item.Sku,
                    Reason = "Stock cannot be negative"
                });
                continue;
            }

            var product = await _productRepository.GetBySKUAsync(item.Sku).ConfigureAwait(false);
            if (product is null)
            {
                failures.Add(new BulkUpdateFailure
                {
                    Sku = item.Sku,
                    Reason = "SKU not found"
                });
                continue;
            }

            var delta = item.NewStock - product.Stock;
            if (delta != 0)
                product.AdjustStock(delta, StockMovementType.Adjustment, "Bulk stock update");
            await _productRepository.UpdateAsync(product).ConfigureAwait(false);
            successCount++;
        }

        if (successCount > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new BulkUpdateResult
        {
            SuccessCount = successCount,
            FailedCount = failures.Count,
            Failures = failures
        };
    }
}
