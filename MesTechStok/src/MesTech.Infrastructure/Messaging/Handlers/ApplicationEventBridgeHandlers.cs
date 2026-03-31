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

// InvoiceApprovedGLBridge + InvoiceCancelledReversalBridge → AccountingEventBridgeHandlers.cs (canonical)
