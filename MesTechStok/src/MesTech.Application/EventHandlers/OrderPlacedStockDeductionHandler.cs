using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Sipariş onaylandığında her kalem için stok düşürür.
/// Zincir 1 kalbi: Platform→Sipariş→Stok düşür→StockChangedEvent→Platform sync.
/// Infrastructure MediatR bridge handler bu servisi çağırır.
/// </summary>
public interface IOrderPlacedEventHandler
{
    Task HandleAsync(Guid orderId, string orderNumber, CancellationToken ct);
}

public class OrderPlacedStockDeductionHandler : IOrderPlacedEventHandler
{
    private readonly IOrderRepository _orderRepo;
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderPlacedStockDeductionHandler> _logger;

    public OrderPlacedStockDeductionHandler(
        IOrderRepository orderRepo,
        IProductRepository productRepo,
        IUnitOfWork unitOfWork,
        ILogger<OrderPlacedStockDeductionHandler> logger)
    {
        _orderRepo = orderRepo;
        _productRepo = productRepo;
        _unitOfWork = unitOfWork;
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

        var failures = new List<string>();

        foreach (var item in order.OrderItems)
        {
            // Safety net: one failed item must not block remaining items in the order
#pragma warning disable CA1031 // Intentional broad catch — per-item resilience in order processing loop
            try
            {
                var product = await _productRepo.GetByIdAsync(item.ProductId);
                if (product is null)
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
