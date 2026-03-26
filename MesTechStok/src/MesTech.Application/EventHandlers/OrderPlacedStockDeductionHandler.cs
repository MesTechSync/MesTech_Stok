using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Sipariş onaylandığında her kalem için stok düşürür.
/// Zincir 1 kalbi: Platform→Sipariş→Stok düşür→StockChangedEvent→Platform sync.
/// DEV6-TUR12: Distributed lock ile overselling koruması eklendi.
/// </summary>
public interface IOrderPlacedEventHandler
{
    Task HandleAsync(Guid orderId, string orderNumber, CancellationToken ct);
}

public sealed class OrderPlacedStockDeductionHandler : IOrderPlacedEventHandler
{
    private readonly IOrderRepository _orderRepo;
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedLockService _lockService;
    private readonly ILogger<OrderPlacedStockDeductionHandler> _logger;

    public OrderPlacedStockDeductionHandler(
        IOrderRepository orderRepo,
        IProductRepository productRepo,
        IUnitOfWork unitOfWork,
        IDistributedLockService lockService,
        ILogger<OrderPlacedStockDeductionHandler> logger)
    {
        _orderRepo = orderRepo;
        _productRepo = productRepo;
        _unitOfWork = unitOfWork;
        _lockService = lockService;
        _logger = logger;
    }

    public async Task HandleAsync(Guid orderId, string orderNumber, CancellationToken ct)
    {
        _logger.LogInformation(
            "OrderPlaced → stok düşürme başlıyor. OrderId={OrderId}, OrderNumber={OrderNumber}",
            orderId, orderNumber);

        var order = await _orderRepo.GetByIdAsync(orderId);
        if (order is null)
        {
            _logger.LogError("Order {OrderId} bulunamadı — stok düşürme atlandı", orderId);
            return;
        }

        // Batch query — N+1 yerine tek SQL (WHERE Id IN (...))
        var productIds = order.OrderItems.Select(i => i.ProductId).Distinct().ToList();

        // FIX-ÖZ-DENETİM: Lock product bazlı olmalı, order bazlı değil.
        // Sorted lock acquisition → deadlock önleme (consistent ordering).
        var sortedProductIds = productIds.OrderBy(id => id).ToList();
        var lockHandles = new List<IAsyncDisposable>();
        try
        {
            foreach (var pid in sortedProductIds)
            {
                var lockHandle = await _lockService.AcquireLockAsync(
                    $"stock:product:{pid}", expiry: TimeSpan.FromSeconds(30),
                    waitTimeout: TimeSpan.FromSeconds(10), ct).ConfigureAwait(false);
                if (lockHandle is null)
                {
                    _logger.LogWarning("Product lock alınamadı — SKU düşürme atlandı. ProductId={ProductId}", pid);
                    // Alınan lock'ları release et
                    foreach (var h in lockHandles) await h.DisposeAsync().ConfigureAwait(false);
                    return;
                }
                lockHandles.Add(lockHandle);
            }

            // Lock'lar alındı — ürünleri yeniden yükle (stale data önleme)
            var products = await _productRepo.GetByIdsAsync(sortedProductIds, ct);
            var productMap = products.ToDictionary(p => p.Id);

            var failures = new List<string>();

            foreach (var item in order.OrderItems)
            {
#pragma warning disable CA1031 // Intentional broad catch — per-item resilience
                try
                {
                    if (!productMap.TryGetValue(item.ProductId, out var product))
                    {
                        _logger.LogWarning("Product {ProductId} bulunamadı — SKU={SKU}",
                            item.ProductId, item.ProductSKU);
                        failures.Add($"{item.ProductSKU}: ürün bulunamadı");
                        continue;
                    }

                    if (product.Stock < item.Quantity)
                    {
                        _logger.LogWarning(
                            "OVERSELLING! SKU={SKU}, Mevcut={Stock}, İstenen={Qty}, Order={OrderNumber}",
                            item.ProductSKU, product.Stock, item.Quantity, orderNumber);
                    }

                    product.AdjustStock(-item.Quantity, StockMovementType.Sale,
                        $"Sipariş #{orderNumber}");

                    _logger.LogInformation(
                        "Stok düşürüldü — SKU={SKU}, Önceki={Prev}, Yeni={New}",
                        item.ProductSKU, product.Stock + item.Quantity, product.Stock);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stok düşürme BAŞARISIZ — SKU={SKU}", item.ProductSKU);
                    failures.Add($"{item.ProductSKU}: {ex.Message}");
                }
#pragma warning restore CA1031
            }

            await _unitOfWork.SaveChangesAsync(ct);

            if (failures.Count > 0)
            {
                _logger.LogWarning(
                    "Sipariş {OrderNumber} — {FailCount}/{TotalCount} kalemde stok düşürme başarısız",
                    orderNumber, failures.Count, order.OrderItems.Count);
            }
        }
        finally
        {
            // Product lock'ları release et (ters sırada — deadlock safety)
            for (var i = lockHandles.Count - 1; i >= 0; i--)
                await lockHandles[i].DisposeAsync().ConfigureAwait(false);
        }
    }
}
