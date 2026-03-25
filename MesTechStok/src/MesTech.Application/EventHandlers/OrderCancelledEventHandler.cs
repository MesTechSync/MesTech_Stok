using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Sipariş iptal edildiğinde stok iadesi ve muhasebe kayıt iptalini tetikler.
/// Infrastructure MediatR bridge handler bu servisi çağırır.
/// </summary>
public interface IOrderCancelledEventHandler
{
    Task HandleAsync(OrderCancelledEvent domainEvent, CancellationToken ct);
}

public sealed class OrderCancelledEventHandler : IOrderCancelledEventHandler
{
    private readonly ILogger<OrderCancelledEventHandler> _logger;

    public OrderCancelledEventHandler(ILogger<OrderCancelledEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderCancelledEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "OrderCancelled: OrderId={OrderId}, Platform={Platform}, Reason={Reason}",
            domainEvent.OrderId, domainEvent.PlatformCode, domainEvent.Reason);

        return Task.CompletedTask;
    }
}
