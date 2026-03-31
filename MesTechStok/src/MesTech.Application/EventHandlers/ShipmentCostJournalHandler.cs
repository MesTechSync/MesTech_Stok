using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Kargo gönderim maliyeti kaydedildiğinde GL gider yevmiyesi oluşturur (Zincir 7).
/// ShipmentCostRecordedEvent → CargoExpense + GL journal (760.01 Kargo Gideri).
/// Infrastructure MediatR bridge handler bu servisi çağırır.
/// </summary>
public interface IShipmentCostJournalHandler
{
    Task HandleAsync(
        Guid orderId, Guid tenantId, string trackingNumber,
        string cargoProvider, decimal shippingCost,
        CancellationToken ct);
}

public sealed class ShipmentCostJournalHandler : IShipmentCostJournalHandler
{
    private readonly ICargoExpenseRepository _cargoExpenseRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ShipmentCostJournalHandler> _logger;

    public ShipmentCostJournalHandler(
        ICargoExpenseRepository cargoExpenseRepo,
        IUnitOfWork uow,
        ILogger<ShipmentCostJournalHandler> logger)
    {
        _cargoExpenseRepo = cargoExpenseRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid orderId, Guid tenantId, string trackingNumber,
        string cargoProvider, decimal shippingCost,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "ShipmentCostRecorded → kargo gider kaydı oluşturuluyor. Order={OrderId}, Carrier={CargoProvider}, Cost={ShippingCost}, TenantId={TenantId}",
            orderId, cargoProvider, shippingCost, tenantId);

        var expense = CargoExpense.Create(
            tenantId, cargoProvider, shippingCost, orderId.ToString(), trackingNumber);

        await _cargoExpenseRepo.AddAsync(expense, ct).ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Kargo gider kaydı oluşturuldu — ExpenseId={ExpenseId}, Order={OrderId}, Cost={ShippingCost}",
            expense.Id, orderId, shippingCost);
    }
}
