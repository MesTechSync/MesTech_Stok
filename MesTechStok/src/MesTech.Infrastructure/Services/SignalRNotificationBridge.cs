using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Events;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// OrderReceivedEvent domain event'ini real-time bildirime donusturen kopru.
/// IDashboardNotifier uzerinden WebSocket/SignalR broadcast yapar.
/// MediatR INotificationHandler olarak calisan event-driven mimari.
/// </summary>
public sealed class SignalRNotificationBridge :
    INotificationHandler<DomainEventNotification<OrderReceivedEvent>>,
    INotificationHandler<DomainEventNotification<LowStockDetectedEvent>>,
    INotificationHandler<DomainEventNotification<InvoiceCreatedEvent>>,
    INotificationHandler<DomainEventNotification<OrderCancelledEvent>>,
    INotificationHandler<DomainEventNotification<BuyboxLostEvent>>,
    INotificationHandler<DomainEventNotification<SyncErrorOccurredEvent>>
{
    private readonly IDashboardNotifier _notifier;
    private readonly ILogger<SignalRNotificationBridge> _logger;

    public SignalRNotificationBridge(
        IDashboardNotifier notifier,
        ILogger<SignalRNotificationBridge> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<OrderReceivedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "SignalR bridge: OrderReceived → broadcast: platform={Platform}, orderId={OrderId}",
            e.PlatformCode, e.PlatformOrderId);

        await _notifier.NotifyNewOrderAsync(
            e.PlatformCode, e.PlatformOrderId, e.TotalAmount, 0, cancellationToken).ConfigureAwait(false);
    }

    public async Task Handle(
        DomainEventNotification<LowStockDetectedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "SignalR bridge: StockAlert → broadcast: sku={SKU}, current={Current}, min={Min}",
            e.SKU, e.CurrentStock, e.MinimumStock);

        await _notifier.NotifyLowStockAsync(
            e.SKU, e.SKU, e.CurrentStock, e.MinimumStock, cancellationToken).ConfigureAwait(false);
    }

    public async Task Handle(
        DomainEventNotification<InvoiceCreatedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "SignalR bridge: InvoiceReady → broadcast: invoiceId={InvoiceId}, total={Total}",
            e.InvoiceId, e.GrandTotal);

        await _notifier.NotifyInvoiceGeneratedAsync(
            e.InvoiceId.ToString(), "Webhook", e.GrandTotal, cancellationToken).ConfigureAwait(false);
    }

    public async Task Handle(
        DomainEventNotification<OrderCancelledEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "SignalR bridge: OrderCancelled → broadcast: platform={Platform}, orderId={OrderId}",
            e.PlatformCode, e.PlatformOrderId);

        // Use return notification channel for cancelled orders
        await _notifier.NotifyReturnCreatedAsync(
            e.PlatformCode,
            e.OrderId.ToString(),
            e.PlatformOrderId,
            e.Reason ?? "Order cancelled via webhook",
            cancellationToken).ConfigureAwait(false);
    }

    public async Task Handle(
        DomainEventNotification<BuyboxLostEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "SignalR bridge: BuyboxLost → broadcast: sku={SKU}, current={Current}, competitor={Competitor}",
            e.SKU, e.CurrentPrice, e.CompetitorPrice);

        await _notifier.NotifyBuyboxLostAsync(
            e.TenantId, e.SKU, e.CurrentPrice, e.CompetitorPrice, e.CompetitorName, cancellationToken).ConfigureAwait(false);
    }

    public async Task Handle(
        DomainEventNotification<SyncErrorOccurredEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogWarning(
            "SignalR bridge: SyncError → broadcast: platform={Platform}, error={ErrorType}, message={Message}",
            e.Platform, e.ErrorType, e.Message);

        await _notifier.NotifySyncStatusAsync(
            e.Platform, $"error:{e.ErrorType}", 0, 0, cancellationToken).ConfigureAwait(false);
    }
}
