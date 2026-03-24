using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// İade onaylandığında stok geri yükleme (Zincir 5 — stok bacağı).
/// ReturnApprovedEvent tetikler → Product.AdjustStock(+qty, CustomerReturn).
/// NOT: ReturnApprovedHandler da stok+GL birleşik yapar. Bu handler
/// ayrıştırılmış sorumluluk (SRP) için eklendi.
/// </summary>
public interface IReturnApprovedStockRestorationHandler
{
    Task HandleAsync(
        Guid returnRequestId, Guid tenantId,
        IReadOnlyList<ReturnLineInfoEvent> lines,
        CancellationToken ct);
}

public class ReturnApprovedStockRestorationHandler : IReturnApprovedStockRestorationHandler
{
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ReturnApprovedStockRestorationHandler> _logger;

    public ReturnApprovedStockRestorationHandler(
        IProductRepository productRepo,
        IUnitOfWork uow,
        ILogger<ReturnApprovedStockRestorationHandler> logger)
    {
        _productRepo = productRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid returnRequestId, Guid tenantId,
        IReadOnlyList<ReturnLineInfoEvent> lines,
        CancellationToken ct)
    {
        foreach (var line in lines)
        {
            var product = await _productRepo.GetByIdAsync(line.ProductId).ConfigureAwait(false);
            if (product is null)
            {
                _logger.LogWarning("İade stok geri — ürün bulunamadı: {ProductId}", line.ProductId);
                continue;
            }

            product.AdjustStock(line.Quantity, StockMovementType.CustomerReturn);

            _logger.LogInformation(
                "Stok geri eklendi — SKU={SKU}, Qty=+{Qty}, YeniStok={NewStock}",
                line.SKU, line.Quantity, product.Stock);
        }

        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "İade stok geri tamamlandı — ReturnId={ReturnId}, Kalem={Count}",
            returnRequestId, lines.Count);
    }
}
