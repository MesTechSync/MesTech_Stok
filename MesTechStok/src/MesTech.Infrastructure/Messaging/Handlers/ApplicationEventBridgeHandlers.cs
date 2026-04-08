using MediatR;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Events;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Handlers;

// ════════════════════════════════════════════════════════════════════════════
// APPLICATION EVENT BRIDGE HANDLERS
// ════════════════════════════════════════════════════════════════════════════
//
// PROBLEM: 52 Application EventHandler interface+class tanımlı ama MediatR
//          dispatch zincirine bağlı DEĞİLDİ. Domain event fırladığında
//          Application handler'lar HİÇ ÇAĞRILMIYORDU.
//
// ÇÖZÜM:  DomainEventNotification<T> → Application interface bridge.
//          Her bridge handler MediatR INotificationHandler implement eder,
//          DI'dan Application handler'ı resolve eder ve çağırır.
//
// MİMARİ: Domain (event) → Infrastructure (bridge) → Application (handler)
//          Clean Architecture korundu: Application, Infrastructure'a bağlanmaz.
//
// BAĞLANAN ZİNCİRLER:
//   Z1: OrderPlaced → StokDüşür (OrderPlacedStockDeductionHandler)
//   Z2: OrderPlaced → GelirKaydı (OrderConfirmedRevenueHandler)
//   Z5a: ReturnApproved → StokGeri (ReturnApprovedStockRestorationHandler)
//   Z5b: ReturnApproved → GLTers (ReturnJournalReversalHandler)
//   Z7: ShipmentCostRecorded → GLGider (ShipmentCostRecordedEventHandler)
//   Z8: ZeroStockDetected → PlatformPasif (ZeroStockDetectedEventHandler)
//   Z10: PriceLossDetected → Uyarı (PriceLossDetectedEventHandler)
//   Z11: StaleOrderDetected → Uyarı (StaleOrderDetectedEventHandler)
//   OrderCancelled → İptalİşlemleri (OrderCancelledEventHandler)
//   OrderShipped → KargoMaliyet (OrderShippedCostHandler)
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Zincir 1: OrderPlacedEvent → Stok düşür.
/// </summary>
public sealed class OrderPlacedStockBridge
    : INotificationHandler<DomainEventNotification<OrderPlacedEvent>>
{
    private readonly IOrderPlacedEventHandler _handler;
    private readonly ILogger<OrderPlacedStockBridge> _logger;

    public OrderPlacedStockBridge(
        IOrderPlacedEventHandler handler,
        ILogger<OrderPlacedStockBridge> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<OrderPlacedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogDebug(
            "[Bridge] OrderPlaced → StockDeduction: OrderId={OrderId}, OrderNumber={OrderNumber}",
            e.OrderId, e.OrderNumber);

        await _handler.HandleAsync(e.OrderId, e.OrderNumber, cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Zincir 2: OrderPlacedEvent → Gelir kaydı oluştur.
/// </summary>
public sealed class OrderPlacedRevenueBridge
    : INotificationHandler<DomainEventNotification<OrderPlacedEvent>>
{
    private readonly IOrderConfirmedRevenueHandler _handler;
    private readonly ILogger<OrderPlacedRevenueBridge> _logger;

    public OrderPlacedRevenueBridge(
        IOrderConfirmedRevenueHandler handler,
        ILogger<OrderPlacedRevenueBridge> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<OrderPlacedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogDebug(
            "[Bridge] OrderPlaced → Revenue: OrderId={OrderId}, Amount={Amount}",
            e.OrderId, e.TotalAmount);

        await _handler.HandleAsync(
            e.OrderId, e.TenantId, e.OrderNumber,
            e.TotalAmount, storeId: null, cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Zincir 5a: ReturnApprovedEvent → Stok geri ekle.
/// </summary>
public sealed class ReturnApprovedStockBridge
    : INotificationHandler<DomainEventNotification<ReturnApprovedEvent>>
{
    private readonly IReturnApprovedStockRestorationHandler _handler;
    private readonly ILogger<ReturnApprovedStockBridge> _logger;

    public ReturnApprovedStockBridge(
        IReturnApprovedStockRestorationHandler handler,
        ILogger<ReturnApprovedStockBridge> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<ReturnApprovedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogDebug(
            "[Bridge] ReturnApproved → StockRestore: ReturnId={ReturnId}, Lines={LineCount}",
            e.ReturnRequestId, e.Lines.Count);

        await _handler.HandleAsync(
            e.ReturnRequestId, e.TenantId, e.Lines, cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Zincir 5b: ReturnApprovedEvent → GL ters yevmiye.
/// </summary>
public sealed class ReturnApprovedJournalBridge
    : INotificationHandler<DomainEventNotification<ReturnApprovedEvent>>
{
    private readonly IReturnJournalReversalHandler _handler;
    private readonly ILogger<ReturnApprovedJournalBridge> _logger;

    public ReturnApprovedJournalBridge(
        IReturnJournalReversalHandler handler,
        ILogger<ReturnApprovedJournalBridge> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<ReturnApprovedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        var totalRefundAmount = e.Lines.Sum(l => l.Quantity * l.UnitPrice);
        _logger.LogDebug(
            "[Bridge] ReturnApproved → JournalReversal: ReturnId={ReturnId}, RefundAmount={Amount}",
            e.ReturnRequestId, totalRefundAmount);

        await _handler.HandleAsync(
            e.ReturnRequestId, e.OrderId, e.TenantId,
            totalRefundAmount, cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Zincir 5c: ReturnApprovedEvent → Ödeme iadesi (refund).
/// </summary>
public sealed class ReturnApprovedRefundBridge
    : INotificationHandler<DomainEventNotification<ReturnApprovedEvent>>
{
    private readonly IReturnApprovedRefundHandler _handler;
    private readonly ILogger<ReturnApprovedRefundBridge> _logger;

    public ReturnApprovedRefundBridge(
        IReturnApprovedRefundHandler handler,
        ILogger<ReturnApprovedRefundBridge> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<ReturnApprovedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        var totalRefundAmount = e.Lines.Sum(l => l.Quantity * l.UnitPrice);
        _logger.LogDebug(
            "[Bridge] ReturnApproved → Refund: ReturnId={ReturnId}, RefundAmount={Amount}",
            e.ReturnRequestId, totalRefundAmount);

        await _handler.HandleAsync(
            e.ReturnRequestId, e.OrderId, e.TenantId,
            totalRefundAmount, cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Zincir 8: ZeroStockDetectedEvent → Platform pasif.
/// Not: ZeroStockPlatformDeactivationHandler zaten Infrastructure'da var ve platform
///      deaktivasyonunu yapar. Bu bridge Application handler'ı çağırır — bildirim/loglama.
/// </summary>
public sealed class ZeroStockApplicationBridge
    : INotificationHandler<DomainEventNotification<ZeroStockDetectedEvent>>
{
    private readonly IZeroStockEventHandler _handler;
    private readonly IIntegrationEventPublisher _publisher;
    private readonly ILogger<ZeroStockApplicationBridge> _logger;

    public ZeroStockApplicationBridge(
        IZeroStockEventHandler handler,
        IIntegrationEventPublisher publisher,
        ILogger<ZeroStockApplicationBridge> logger)
    {
        _handler = handler;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<ZeroStockDetectedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogDebug(
            "[Bridge] ZeroStockDetected → Application: SKU={SKU}", e.SKU);

        await _handler.HandleAsync(e.ProductId, e.SKU, e.TenantId, cancellationToken)
            .ConfigureAwait(false);

        await _publisher.PublishZeroStockDetectedAsync(
            e.ProductId, e.SKU, e.PreviousStock, cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Zincir 11: StaleOrderDetectedEvent → Integration event publish.
/// Job zaten MediatR Publish ile domain event fırlatıyor,
/// bu bridge integration event'i dış tüketicilere yayınlar.
/// </summary>
public sealed class StaleOrderIntegrationBridge
    : INotificationHandler<DomainEventNotification<StaleOrderDetectedEvent>>
{
    private readonly IIntegrationEventPublisher _publisher;
    private readonly ILogger<StaleOrderIntegrationBridge> _logger;

    public StaleOrderIntegrationBridge(
        IIntegrationEventPublisher publisher,
        ILogger<StaleOrderIntegrationBridge> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<StaleOrderDetectedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogDebug(
            "[Bridge] StaleOrderDetected → Integration: Order={OrderNumber}, Elapsed={Hours}h",
            e.OrderNumber, e.ElapsedSince.TotalHours);

        await _publisher.PublishStaleOrderDetectedAsync(
            e.OrderId, e.OrderNumber, e.Platform?.ToString(),
            e.ElapsedSince.TotalHours, cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Zincir 15: OversellingAttemptedEvent → Audit log + alarm.
/// Stok yetersiz sipariş girişiminde fırlatılır.
/// </summary>
public sealed class OversellingAttemptedBridge
    : INotificationHandler<DomainEventNotification<OversellingAttemptedEvent>>
{
    private readonly IOversellingAttemptedEventHandler _handler;
    private readonly ILogger<OversellingAttemptedBridge> _logger;

    public OversellingAttemptedBridge(
        IOversellingAttemptedEventHandler handler,
        ILogger<OversellingAttemptedBridge> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<OversellingAttemptedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogDebug(
            "[Bridge] OversellingAttempted → Audit: SKU={SKU}, Available={Available}, Requested={Requested}",
            e.SKU, e.AvailableStock, e.RequestedQuantity);

        await _handler.HandleAsync(
            e.ProductId, e.SKU, e.TenantId,
            e.AvailableStock, e.RequestedQuantity, e.OrderNumber,
            cancellationToken).ConfigureAwait(false);
    }
}

// ════════════════════════════════════════════════════════════════════════════
// DEV1 TUR — Eksik bridge handler'lar
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// OrderPlacedEvent → CariHesap otomatik olusturma.
/// </summary>
public sealed class OrderPlacedCariHesapBridge
    : INotificationHandler<DomainEventNotification<OrderPlacedEvent>>
{
    private readonly IOrderPlacedCariHesapHandler _handler;
    private readonly ILogger<OrderPlacedCariHesapBridge> _logger;

    public OrderPlacedCariHesapBridge(IOrderPlacedCariHesapHandler handler, ILogger<OrderPlacedCariHesapBridge> logger)
    { _handler = handler; _logger = logger; }

    public async Task Handle(DomainEventNotification<OrderPlacedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogDebug("[Bridge] OrderPlaced → CariHesap: OrderId={OrderId}", e.OrderId);
        await _handler.HandleAsync(e.OrderId, e.TenantId, ct).ConfigureAwait(false);
    }
}

/// <summary>
/// OrderCompletedEvent → Otomatik fatura olusturma (Z2→Z3).
/// </summary>
public sealed class OrderCompletedInvoiceBridge
    : INotificationHandler<DomainEventNotification<OrderCompletedEvent>>
{
    private readonly IOrderCompletedInvoiceHandler _handler;
    private readonly ILogger<OrderCompletedInvoiceBridge> _logger;

    public OrderCompletedInvoiceBridge(IOrderCompletedInvoiceHandler handler, ILogger<OrderCompletedInvoiceBridge> logger)
    { _handler = handler; _logger = logger; }

    public async Task Handle(DomainEventNotification<OrderCompletedEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogDebug("[Bridge] OrderCompleted → Invoice: Order={OrderNumber}, Amount={Amount}", e.OrderNumber, e.TotalAmount);
        await _handler.HandleAsync(e.OrderId, e.TenantId, e.OrderNumber, e.TotalAmount, ct).ConfigureAwait(false);
    }
}

/// <summary>
/// Z5a: OrderCancelledEvent → stok geri yukleme.
/// </summary>
public sealed class OrderCancelledStockRestorationBridge
    : INotificationHandler<DomainEventNotification<OrderCancelledEvent>>
{
    private readonly IOrderCancelledStockRestorationHandler _handler;
    private readonly ILogger<OrderCancelledStockRestorationBridge> _logger;

    public OrderCancelledStockRestorationBridge(
        IOrderCancelledStockRestorationHandler handler,
        ILogger<OrderCancelledStockRestorationBridge> logger)
    { _handler = handler; _logger = logger; }

    public async Task Handle(DomainEventNotification<OrderCancelledEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        _logger.LogDebug("[Bridge] OrderCancelled → StockRestoration: OrderId={OrderId}", e.OrderId);
        await _handler.HandleAsync(e.OrderId, e.TenantId, e.Reason, ct).ConfigureAwait(false);
    }
}

// InvoiceApprovedGLBridge + InvoiceCancelledReversalBridge → AccountingEventBridgeHandlers.cs (canonical)
