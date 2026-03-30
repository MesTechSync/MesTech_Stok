using MesTech.Application.EventHandlers;
using MesTech.Domain.Events;
using MesTech.Infrastructure.Messaging.Mesa;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Orchestration;

/// <summary>
/// Sipariş tamamlandığında Application handler'a yönlendirir.
/// Zincir Z2: OrderCompletedEvent → gelir kaydı tamamlama + bildirim.
/// DomainEventNotification wrapper ile MediatR'dan tetiklenir.
/// </summary>
public sealed class OrderCompletedEventHandler
    : INotificationHandler<DomainEventNotification<OrderCompletedEvent>>
{
    private readonly IOrderCompletedNotificationHandler _handler;
    private readonly ILogger<OrderCompletedEventHandler> _logger;

    public OrderCompletedEventHandler(
        IOrderCompletedNotificationHandler handler,
        ILogger<OrderCompletedEventHandler> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<OrderCompletedEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.DomainEvent;

        _logger.LogInformation(
            "OrderCompletedEvent alındı — Order={OrderNumber}, Tutar={Amount}, Müşteri={Customer}",
            evt.OrderNumber, evt.TotalAmount, evt.CustomerName);

        try
        {
            await _handler.HandleAsync(
                evt.OrderId, evt.TenantId, evt.OrderNumber,
                evt.TotalAmount, evt.CustomerName,
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Sipariş tamamlama bildirimi gönderildi — Order={OrderNumber}",
                evt.OrderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Sipariş tamamlama handler hatası — Order={OrderNumber}",
                evt.OrderNumber);
        }
    }
}
