using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

public interface IStockCriticalEventHandler
{
    Task HandleAsync(StockCriticalEvent domainEvent, CancellationToken ct);
}

public class StockCriticalEventHandler : IStockCriticalEventHandler
{
    private readonly ILogger<StockCriticalEventHandler> _logger;

    public StockCriticalEventHandler(ILogger<StockCriticalEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(StockCriticalEvent domainEvent, CancellationToken ct)
    {
        _logger.LogWarning(
            "StockCritical: ProductId={ProductId}, SKU={SKU}, Stock={Current}/{Min}, Level={Level}, Warehouse={Warehouse}",
            domainEvent.ProductId, domainEvent.SKU, domainEvent.CurrentStock,
            domainEvent.MinimumStock, domainEvent.Level, domainEvent.WarehouseName);

        return Task.CompletedTask;
    }
}
