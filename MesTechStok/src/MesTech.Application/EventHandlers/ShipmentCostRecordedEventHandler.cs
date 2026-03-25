using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IShipmentCostRecordedEventHandler
{
    Task HandleAsync(ShipmentCostRecordedEvent domainEvent, CancellationToken ct);
}

public sealed class ShipmentCostRecordedEventHandler : IShipmentCostRecordedEventHandler
{
    private readonly ILogger<ShipmentCostRecordedEventHandler> _logger;

    public ShipmentCostRecordedEventHandler(ILogger<ShipmentCostRecordedEventHandler> logger)
        => _logger = logger;

    public Task HandleAsync(ShipmentCostRecordedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "Kargo gider kaydı — OrderId={OrderId}, CargoProvider={Provider}, Cost={Cost}",
            domainEvent.OrderId, domainEvent.CargoProvider, domainEvent.ShippingCost);
        return Task.CompletedTask;
    }
}
