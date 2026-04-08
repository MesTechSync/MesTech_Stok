using MediatR;
using MesTech.Application.Features.Notifications.Commands.SendNotification;
using MesTech.Infrastructure.Messaging.Mesa;
using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Handlers;

/// <summary>
/// OrderShippedEvent handler — kargo takip loglama + müşteri bildirim dispatch.
/// </summary>
public sealed class OrderShippedLogHandler : INotificationHandler<DomainEventNotification<OrderShippedEvent>>
{
    private readonly IMediator _mediator;
    private readonly IIntegrationEventPublisher _publisher;
    private readonly ILogger<OrderShippedLogHandler> _logger;

    public OrderShippedLogHandler(
        IMediator mediator,
        IIntegrationEventPublisher publisher,
        ILogger<OrderShippedLogHandler> logger)
    {
        _mediator = mediator;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<OrderShippedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] OrderShipped — OrderId={OrderId}, Tracking={Tracking}, Provider={Provider}",
            e.OrderId, e.TrackingNumber, e.CargoProvider);

        await _publisher.PublishOrderShippedAsync(
            e.OrderId, e.TrackingNumber, e.CargoProvider.ToString(), ct)
            .ConfigureAwait(false);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "order-shipped",
                Content: $"Sipariş kargoya verildi.\n" +
                         $"Takip No: {e.TrackingNumber}\n" +
                         $"Kargo: {e.CargoProvider}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OrderShipped bildirim gönderilemedi — OrderId={OrderId}", e.OrderId);
        }
    }
}

/// <summary>
/// StockCriticalEvent handler — kritik stok seviyesi uyarısı + bildirim dispatch.
/// </summary>
public sealed class StockCriticalLogHandler : INotificationHandler<DomainEventNotification<StockCriticalEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<StockCriticalLogHandler> _logger;

    public StockCriticalLogHandler(IMediator mediator, ILogger<StockCriticalLogHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<StockCriticalEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogWarning(
            "[Event] StockCritical — Product={SKU} ({Name}), Stock={Stock}, Minimum={MinStock}, Level={Level}",
            e.SKU, e.ProductName, e.CurrentStock, e.MinimumStock, e.Level);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "stock-critical",
                Content: $"⚠️ Kritik stok: {e.SKU} ({e.ProductName})\n" +
                         $"Mevcut: {e.CurrentStock} / Minimum: {e.MinimumStock}\n" +
                         $"Seviye: {e.Level}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "StockCritical bildirim gönderilemedi — SKU={SKU}", e.SKU);
        }
    }
}

/// <summary>
/// InvoiceApprovedEvent handler — fatura onay loglama + bildirim dispatch.
/// </summary>
public sealed class InvoiceApprovedLogHandler : INotificationHandler<DomainEventNotification<InvoiceApprovedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<InvoiceApprovedLogHandler> _logger;

    public InvoiceApprovedLogHandler(IMediator mediator, ILogger<InvoiceApprovedLogHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<InvoiceApprovedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] InvoiceApproved — InvoiceId={InvoiceId}, Number={Number}, Total={Total}, Type={Type}",
            e.InvoiceId, e.InvoiceNumber, e.GrandTotal, e.Type);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "invoice-approved",
                Content: $"Fatura onaylandı: {e.InvoiceNumber}\n" +
                         $"Toplam: {e.GrandTotal:C}\n" +
                         $"Tip: {e.Type}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "InvoiceApproved bildirim gönderilemedi — {InvoiceId}", e.InvoiceId);
        }
    }
}

/// <summary>
/// InvoiceAcceptedEvent handler — fatura kabul loglama + bildirim dispatch.
/// </summary>
public sealed class InvoiceAcceptedLogHandler : INotificationHandler<DomainEventNotification<InvoiceAcceptedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<InvoiceAcceptedLogHandler> _logger;

    public InvoiceAcceptedLogHandler(IMediator mediator, ILogger<InvoiceAcceptedLogHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<InvoiceAcceptedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "[Event] InvoiceAccepted — InvoiceId={InvoiceId}, Number={Number}, Total={Total}",
            e.InvoiceId, e.InvoiceNumber, e.GrandTotal);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "invoice-accepted",
                Content: $"Fatura kabul edildi: {e.InvoiceNumber}\n" +
                         $"Toplam: {e.GrandTotal:C}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "InvoiceAccepted bildirim gönderilemedi — {InvoiceId}", e.InvoiceId);
        }
    }
}

/// <summary>
/// InvoiceRejectedEvent handler — fatura red loglama + bildirim dispatch.
/// </summary>
public sealed class InvoiceRejectedLogHandler : INotificationHandler<DomainEventNotification<InvoiceRejectedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<InvoiceRejectedLogHandler> _logger;

    public InvoiceRejectedLogHandler(IMediator mediator, ILogger<InvoiceRejectedLogHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<InvoiceRejectedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogWarning(
            "[Event] InvoiceRejected — InvoiceId={InvoiceId}, Number={Number}",
            e.InvoiceId, e.InvoiceNumber);

        try
        {
            await _mediator.Send(new SendNotificationCommand(
                TenantId: e.TenantId,
                Channel: "System",
                Recipient: "tenant-admins",
                TemplateName: "invoice-rejected",
                Content: $"Fatura reddedildi: {e.InvoiceNumber}"), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "InvoiceRejected bildirim gönderilemedi — {InvoiceId}", e.InvoiceId);
        }
    }
}
