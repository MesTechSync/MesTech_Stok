using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Sipariş kargoya verildiğinde takip bilgisi kaydeder.
/// Infrastructure MediatR bridge handler bu servisi çağırır.
/// </summary>
public interface IOrderShippedEventHandler
{
    Task HandleAsync(OrderShippedEvent domainEvent, CancellationToken ct);
}

public class OrderShippedEventHandler : IOrderShippedEventHandler
{
    private readonly ILogger<OrderShippedEventHandler> _logger;

    public OrderShippedEventHandler(ILogger<OrderShippedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderShippedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "OrderShipped: OrderId={OrderId}, Tracking={Tracking}, Cargo={Cargo}",
            domainEvent.OrderId, domainEvent.TrackingNumber, domainEvent.CargoProvider);

        return Task.CompletedTask;
    }
}
