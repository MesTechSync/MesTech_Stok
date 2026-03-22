using MediatR;
using MesTech.Infrastructure.Messaging.Mesa;
using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Handlers;

/// <summary>
/// OrderShippedEvent handler — kargo takip ve müşteri bilgilendirme tetikler.
/// Phase 2'de MESA bridge + kargo API entegrasyonu eklenecek.
/// </summary>
public class OrderShippedLogHandler : INotificationHandler<DomainEventNotification<OrderShippedEvent>>
{
    private readonly ILogger<OrderShippedLogHandler> _logger;

    public OrderShippedLogHandler(ILogger<OrderShippedLogHandler> logger) => _logger = logger;

    public Task Handle(DomainEventNotification<OrderShippedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] OrderShipped — OrderId={OrderId}, Tracking={Tracking}, Provider={Provider}",
            e.OrderId, e.TrackingNumber, e.CargoProvider);
        return Task.CompletedTask;
    }
}

/// <summary>
/// StockCriticalEvent handler — kritik stok seviyesi uyarısı.
/// Phase 2'de notification service + Telegram/Email alert entegrasyonu eklenecek.
/// </summary>
public class StockCriticalLogHandler : INotificationHandler<DomainEventNotification<StockCriticalEvent>>
{
    private readonly ILogger<StockCriticalLogHandler> _logger;

    public StockCriticalLogHandler(ILogger<StockCriticalLogHandler> logger) => _logger = logger;

    public Task Handle(DomainEventNotification<StockCriticalEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogWarning(
            "[Event] StockCritical — Product={SKU} ({Name}), Stock={Stock}, Minimum={MinStock}, Level={Level}",
            e.SKU, e.ProductName, e.CurrentStock, e.MinimumStock, e.Level);
        return Task.CompletedTask;
    }
}

/// <summary>
/// InvoiceApprovedEvent handler — fatura onaylandığında loglama.
/// Phase 2'de MESA bridge + e-Fatura gönderim kuyruğu entegrasyonu eklenecek.
/// </summary>
public class InvoiceApprovedLogHandler : INotificationHandler<DomainEventNotification<InvoiceApprovedEvent>>
{
    private readonly ILogger<InvoiceApprovedLogHandler> _logger;

    public InvoiceApprovedLogHandler(ILogger<InvoiceApprovedLogHandler> logger) => _logger = logger;

    public Task Handle(DomainEventNotification<InvoiceApprovedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] InvoiceApproved — InvoiceId={InvoiceId}, Number={Number}, Total={Total}, Type={Type}",
            e.InvoiceId, e.InvoiceNumber, e.GrandTotal, e.Type);
        return Task.CompletedTask;
    }
}
