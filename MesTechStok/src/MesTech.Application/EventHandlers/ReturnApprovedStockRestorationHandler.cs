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

public sealed class ReturnApprovedStockRestorationHandler : IReturnApprovedStockRestorationHandler
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
        // Batch query — N+1 yerine tek SQL
        var productIds = lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _productRepo.GetByIdsAsync(productIds, ct).ConfigureAwait(false);
        var productMap = products.ToDictionary(p => p.Id);

        foreach (var line in lines)
        {
            if (!productMap.TryGetValue(line.ProductId, out var product))
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
