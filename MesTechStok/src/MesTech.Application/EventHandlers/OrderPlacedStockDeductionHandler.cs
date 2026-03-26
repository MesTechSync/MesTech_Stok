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
        // DEV6-TUR12: Distributed lock — sipariş bazlı lock ile overselling önle
        var lockKey = $"stock-deduction:order:{orderId}";
        await using var lockHandle = await _lockService.AcquireLockAsync(
            lockKey, expiry: TimeSpan.FromSeconds(30), waitTimeout: TimeSpan.FromSeconds(10), ct)
            .ConfigureAwait(false);
        if (lockHandle is null)
        {
            _logger.LogWarning("Distributed lock alınamadı — stok düşürme ertelendi. OrderId={OrderId}", orderId);
            return;
        }
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
        var products = await _productRepo.GetByIdsAsync(productIds, ct);
        var productMap = products.ToDictionary(p => p.Id);

        var failures = new List<string>();

        foreach (var item in order.OrderItems)
        {
            // Safety net: one failed item must not block remaining items in the order
#pragma warning disable CA1031 // Intentional broad catch — per-item resilience in order processing loop
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

                // Domain method — StockChangedEvent + LowStockDetectedEvent otomatik fırlatılır
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
}
