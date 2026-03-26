using MediatR;
using MesTech.Application.DTOs;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.BulkUpdatePrice;

public sealed class BulkUpdatePriceHandler : IRequestHandler<BulkUpdatePriceCommand, BulkUpdateResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BulkUpdatePriceHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<BulkUpdateResult> Handle(BulkUpdatePriceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Batch query — N+1 yerine tek SQL
        var allSkus = request.Items.Select(i => i.Sku).Distinct().ToList();
        var products = await _productRepository.GetBySKUsAsync(allSkus, cancellationToken).ConfigureAwait(false);
        var productMap = products.ToDictionary(p => p.SKU, StringComparer.OrdinalIgnoreCase);

        var failures = new List<BulkUpdateFailure>();
        int successCount = 0;

        foreach (var item in request.Items)
        {
            if (item.NewPrice <= 0)
            {
                failures.Add(new BulkUpdateFailure
                {
                    Sku = item.Sku,
                    Reason = "Price must be greater than 0"
                });
                continue;
            }

            if (!productMap.TryGetValue(item.Sku, out var product))
            {
                failures.Add(new BulkUpdateFailure
                {
                    Sku = item.Sku,
                    Reason = "SKU not found"
                });
                continue;
            }

            product.UpdatePrice(item.NewPrice);
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
