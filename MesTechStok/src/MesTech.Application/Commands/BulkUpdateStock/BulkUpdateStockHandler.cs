using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Constants;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Commands.BulkUpdateStock;

public sealed class BulkUpdateStockHandler : IRequestHandler<BulkUpdateStockCommand, BulkUpdateResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedLockService _lockService;
    private readonly ILogger<BulkUpdateStockHandler> _logger;

    public BulkUpdateStockHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IDistributedLockService lockService,
        ILogger<BulkUpdateStockHandler> logger)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _lockService = lockService ?? throw new ArgumentNullException(nameof(lockService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BulkUpdateResult> Handle(BulkUpdateStockCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Batch query — N+1 yerine tek SQL
        var allSkus = request.Items.Select(i => i.Sku).Distinct().ToList();
        var products = await _productRepository.GetBySKUsAsync(allSkus, cancellationToken).ConfigureAwait(false);
        var productMap = products.ToDictionary(p => p.SKU, StringComparer.OrdinalIgnoreCase);

        // Sorted lock — deadlock prevention (aynı sırada kilitle)
        var productIds = products.Select(p => p.Id).OrderBy(id => id).ToList();
        var lockHandles = new List<IAsyncDisposable>();

        try
        {
            foreach (var pid in productIds)
            {
                var lockHandle = await _lockService.AcquireLockAsync(
                    $"stock:product:{pid}",
                    expiry: DomainConstants.BulkStockLockExpiry,
                    waitTimeout: DomainConstants.BulkStockLockWaitTimeout,
                    cancellationToken).ConfigureAwait(false);

                if (lockHandle is null)
                {
                    _logger.LogWarning("Bulk stock lock alınamadı — ProductId={ProductId}", pid);
                    foreach (var h in lockHandles) await h.DisposeAsync().ConfigureAwait(false);
                    return new BulkUpdateResult
                    {
                        SuccessCount = 0,
                        FailedCount = request.Items.Count,
                        Failures = new List<BulkUpdateFailure>
                        {
                            new() { Sku = "*", Reason = "Stok kilidi alınamadı. Lütfen tekrar deneyin." }
                        }
                    };
                }
                lockHandles.Add(lockHandle);
            }

            // Lock alındıktan sonra güncel veriyi yeniden oku
            products = await _productRepository.GetBySKUsAsync(allSkus, cancellationToken).ConfigureAwait(false);
            productMap = products.ToDictionary(p => p.SKU, StringComparer.OrdinalIgnoreCase);

            var failures = new List<BulkUpdateFailure>();
            int successCount = 0;

            foreach (var item in request.Items)
            {
                if (item.NewStock < 0)
                {
                    failures.Add(new BulkUpdateFailure { Sku = item.Sku, Reason = "Stock cannot be negative" });
                    continue;
                }

                if (!productMap.TryGetValue(item.Sku, out var product))
                {
                    failures.Add(new BulkUpdateFailure { Sku = item.Sku, Reason = "SKU not found" });
                    continue;
                }

                var delta = item.NewStock - product.Stock;
                if (delta != 0)
                    product.AdjustStock(delta, StockMovementType.Adjustment, "Bulk stock update");
                await _productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
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
        finally
        {
            for (var i = lockHandles.Count - 1; i >= 0; i--)
                await lockHandles[i].DisposeAsync().ConfigureAwait(false);
        }
    }
}
