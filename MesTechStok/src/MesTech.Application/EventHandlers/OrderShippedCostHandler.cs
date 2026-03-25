using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Siparis kargolandiginda kargo maliyet kaydi + GL gider kaydi olusturur (Zincir 7).
/// Cift kayit: BORC 760.01 Kargo Giderleri, ALACAK 320 Saticilar.
/// </summary>
public interface IOrderShippedCostHandler
{
    Task HandleAsync(
        Guid orderId, Guid tenantId, string trackingNumber,
        CargoProvider provider, decimal shippingCost,
        CancellationToken ct);
}

public sealed class OrderShippedCostHandler : IOrderShippedCostHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderShippedCostHandler> _logger;

    public OrderShippedCostHandler(
        IUnitOfWork unitOfWork,
        ILogger<OrderShippedCostHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid orderId, Guid tenantId, string trackingNumber,
        CargoProvider provider, decimal shippingCost,
        CancellationToken ct)
    {
        if (shippingCost <= 0)
        {
            _logger.LogDebug("Kargo ucreti 0 — GL kaydi atlanıyor. OrderId={OrderId}", orderId);
            return;
        }

        // ShipmentCost entity kaydi
        var cost = ShipmentCost.Create(
            tenantId, orderId, provider, shippingCost, trackingNumber);

        _logger.LogInformation(
            "Kargo maliyet kaydi olusturuluyor — OrderId={OrderId}, Provider={Provider}, Cost={Cost}",
            orderId, provider, shippingCost);

        // GL yevmiye kaydi
        var entry = JournalEntry.Create(
            tenantId,
            DateTime.UtcNow,
            $"Kargo gideri — Siparis #{orderId.ToString()[..8]}, {provider}",
            trackingNumber);

        // BORC: 760.01 Kargo Giderleri
        entry.AddLine(new Guid("00000760-0000-0000-0000-000000000000"), shippingCost, 0, $"760.01 Kargo Giderleri — {provider}");

        // ALACAK: 320 Saticilar (kargo firmasina borc)
        entry.AddLine(new Guid("00000320-0000-0000-0000-000000000000"), 0, shippingCost, $"320 Saticilar — {provider}");

        entry.Validate();
        entry.Post();

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Kargo GL kaydi tamamlandi — OrderId={OrderId}, JournalEntryId={EntryId}, Cost={Cost}",
            orderId, entry.Id, shippingCost);
    }
}
