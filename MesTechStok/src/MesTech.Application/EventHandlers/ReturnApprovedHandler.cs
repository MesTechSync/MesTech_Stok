using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Iade onaylandiginda:
/// 1. Stok geri ekle (Zincir 5) — Product.AdjustStock(+qty, Return)
/// 2. Ters GL kaydi (Zincir 4) — BORC 610/ALACAK 120
/// </summary>
public interface IReturnApprovedHandler
{
    Task HandleAsync(
        Guid returnRequestId, Guid orderId, Guid tenantId,
        IReadOnlyList<ReturnLineInfoEvent> lines,
        CancellationToken ct);
}

public class ReturnApprovedHandler : IReturnApprovedHandler
{
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReturnApprovedHandler> _logger;

    public ReturnApprovedHandler(
        IProductRepository productRepo,
        IUnitOfWork unitOfWork,
        ILogger<ReturnApprovedHandler> logger)
    {
        _productRepo = productRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid returnRequestId, Guid orderId, Guid tenantId,
        IReadOnlyList<ReturnLineInfoEvent> lines,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Iade onaylandi — stok geri + ters GL baslıyor. ReturnId={ReturnId}, Kalem={Count}",
            returnRequestId, lines.Count);

        decimal totalRefund = 0;

        // Zincir 5: Her kalem icin stok geri ekle
        foreach (var line in lines)
        {
            var product = await _productRepo.GetByIdAsync(line.ProductId).ConfigureAwait(false);
            if (product is null)
            {
                _logger.LogWarning("Iade stok geri — urun bulunamadi: {ProductId}", line.ProductId);
                continue;
            }

            product.AdjustStock(line.Quantity, StockMovementType.CustomerReturn);
            totalRefund += line.UnitPrice * line.Quantity;

            _logger.LogInformation(
                "Stok geri eklendi — SKU={SKU}, Qty=+{Qty}, YeniStok={NewStock}",
                line.SKU, line.Quantity, product.Stock);
        }

        // Zincir 4: Ters GL kaydi (satis iade)
        if (totalRefund > 0)
        {
            var entry = JournalEntry.Create(
                tenantId,
                DateTime.UtcNow,
                $"Satis iadesi — Siparis #{orderId.ToString()[..8]}, Iade #{returnRequestId.ToString()[..8]}",
                $"RET-{returnRequestId.ToString()[..8]}");

            // BORC: 610 Satistan Iadeler (gelir azalisi)
            entry.AddLine(Guid.Empty, totalRefund, 0, "610 Satistan Iadeler");

            // ALACAK: 120 Alicilar (musteri borcundan duser)
            entry.AddLine(Guid.Empty, 0, totalRefund, "120 Alicilar — iade kesintisi");

            entry.Validate();
            entry.Post();
        }

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Iade tamamlandi — ReturnId={ReturnId}, StokGeri={Lines} kalem, TersGL={Refund}",
            returnRequestId, lines.Count, totalRefund);
    }
}
