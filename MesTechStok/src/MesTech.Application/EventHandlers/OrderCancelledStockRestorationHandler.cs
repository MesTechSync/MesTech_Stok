using MesTech.Application.Interfaces;
using MesTech.Domain.Constants;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Sipariş iptal edildiğinde her kalem için stok geri yükler.
/// OrderCancelledEvent → Stok artır (AdjustStock +qty, StockMovementType.Return).
/// Z1 tamamlayıcı: OrderPlacedStockDeductionHandler'ın tersi.
/// </summary>
public interface IOrderCancelledStockRestorationHandler
{
    Task HandleAsync(Guid orderId, Guid tenantId, string? reason, CancellationToken ct);
}

public sealed class OrderCancelledStockRestorationHandler : IOrderCancelledStockRestorationHandler
{
    private readonly IOrderRepository _orderRepo;
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedLockService _lockService;
    private readonly ILogger<OrderCancelledStockRestorationHandler> _logger;

    public OrderCancelledStockRestorationHandler(
        IOrderRepository orderRepo,
        IProductRepository productRepo,
        IUnitOfWork unitOfWork,
        IDistributedLockService lockService,
        ILogger<OrderCancelledStockRestorationHandler> logger)
    {
        _orderRepo = orderRepo;
        _productRepo = productRepo;
        _unitOfWork = unitOfWork;
        _lockService = lockService;
        _logger = logger;
    }

    public async Task HandleAsync(Guid orderId, Guid tenantId, string? reason, CancellationToken ct)
    {
        _logger.LogInformation(
            "OrderCancelled → stok geri yükleme başlıyor. OrderId={OrderId}, Reason={Reason}",
            orderId, reason);

        var order = await _orderRepo.GetByIdAsync(orderId);
        if (order is null)
        {
            _logger.LogError("Order {OrderId} bulunamadı — stok geri y��kleme atlandı", orderId);
            return;
        }

        var productIds = order.OrderItems.Select(i => i.ProductId).Distinct().ToList();
        var sortedProductIds = productIds.OrderBy(id => id).ToList();
        var lockHandles = new List<IAsyncDisposable>();

        try
        {
            foreach (var pid in sortedProductIds)
            {
                var lockHandle = await _lockService.AcquireLockAsync(
                    $"stock:product:{pid}", expiry: DomainConstants.StockLockExpiry,
                    waitTimeout: DomainConstants.StockLockWaitTimeout, ct).ConfigureAwait(false);
                if (lockHandle is null)
                {
                    _logger.LogWarning("Product lock alınamadı — stok geri yükleme atlandı. ProductId={ProductId}", pid);
                    foreach (var h in lockHandles) await h.DisposeAsync().ConfigureAwait(false);
                    return;
                }
                lockHandles.Add(lockHandle);
            }

            var products = await _productRepo.GetByIdsAsync(sortedProductIds, ct);
            var productMap = products.ToDictionary(p => p.Id);

            foreach (var item in order.OrderItems)
            {
#pragma warning disable CA1031 // Intentional broad catch — per-item resilience
                try
                {
                    if (!productMap.TryGetValue(item.ProductId, out var product))
                    {
                        _logger.LogWarning("Product {ProductId} bulunamadı — SKU={SKU}, stok geri yükleme atlandı",
                            item.ProductId, item.ProductSKU);
                        continue;
                    }

                    product.AdjustStock(item.Quantity, StockMovementType.CustomerReturn,
                        $"İptal — Sipariş #{order.OrderNumber}, Sebep: {reason ?? "belirtilmedi"}");

                    _logger.LogInformation(
                        "Stok geri yüklendi — SKU={SKU}, Miktar=+{Qty}, Yeni={New}",
                        item.ProductSKU, item.Quantity, product.Stock);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stok geri yükleme BAŞARISIZ — SKU={SKU}", item.ProductSKU);
                }
#pragma warning restore CA1031
            }

            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "OrderCancelled stok geri yükleme tamamlandı — OrderId={OrderId}, {ItemCount} kalem",
                orderId, order.OrderItems.Count);
        }
        finally
        {
            for (var i = lockHandles.Count - 1; i >= 0; i--)
                await lockHandles[i].DisposeAsync().ConfigureAwait(false);
        }
    }
}
