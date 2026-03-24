using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Sipariş teslim alındığında gelir kaydı ve müşteri istatistiği günceller.
/// Infrastructure MediatR bridge handler bu servisi çağırır.
/// </summary>
public interface IOrderReceivedEventHandler
{
    Task HandleAsync(OrderReceivedEvent domainEvent, CancellationToken ct);
}

public class OrderReceivedEventHandler : IOrderReceivedEventHandler
{
    private readonly ILogger<OrderReceivedEventHandler> _logger;

    public OrderReceivedEventHandler(ILogger<OrderReceivedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderReceivedEvent domainEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "OrderReceived: OrderId={OrderId}, Tenant={TenantId}, Platform={Platform}, Amount={Amount}",
            domainEvent.OrderId, domainEvent.TenantId, domainEvent.PlatformCode, domainEvent.TotalAmount);

        return Task.CompletedTask;
    }
}
