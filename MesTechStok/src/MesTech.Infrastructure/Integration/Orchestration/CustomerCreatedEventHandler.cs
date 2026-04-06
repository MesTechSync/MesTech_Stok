using MesTech.Domain.Entities;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa;

#pragma warning disable CA1711 // DDD event handler — INotificationHandler<DomainEventNotification<T>> pattern
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Orchestration;

/// <summary>
/// Yeni müşteri oluşturulduğunda bildirim + CRM hook tetikler.
/// CustomerCreatedEvent → NotificationLog + CRM pipeline entegrasyonu.
/// </summary>
public sealed class CustomerCreatedEventHandler
    : INotificationHandler<DomainEventNotification<CustomerCreatedEvent>>
{
    private readonly INotificationLogRepository _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CustomerCreatedEventHandler> _logger;

    public CustomerCreatedEventHandler(
        INotificationLogRepository notificationRepo,
        IUnitOfWork uow,
        ILogger<CustomerCreatedEventHandler> logger)
    {
        _notificationRepo = notificationRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<CustomerCreatedEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.DomainEvent;

        _logger.LogInformation(
            "CustomerCreatedEvent alındı — Customer={Name}, Email={Email}",
            evt.CustomerName, evt.Email);

        try
        {
            var log = NotificationLog.Create(
                evt.TenantId,
                MesTech.Domain.Enums.NotificationChannel.Push,
                "System",
                "CustomerCreated",
                $"Yeni müşteri: {evt.CustomerName} | Email={evt.Email ?? "—"} | Tel={evt.Phone ?? "—"}");

            await _notificationRepo.AddAsync(log, cancellationToken).ConfigureAwait(false);
            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Müşteri oluşturma bildirimi kaydedildi — Customer={Name}",
                evt.CustomerName);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Müşteri oluşturma handler hatası — Customer={Name}",
                evt.CustomerName);
        }
    }
}
