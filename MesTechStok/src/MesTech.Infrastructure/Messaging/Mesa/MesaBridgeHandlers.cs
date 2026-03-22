using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa;

/// <summary>
/// MediatR domain event'leri dinler ve MESA-spesifik integration event'lere
/// donusturup MesaEventPublisher ile RabbitMQ'ya publish eder.
/// DomainEventNotification wrapper kullanir — Domain katmani INotification bilmez.
/// </summary>
public class ProductCreatedBridgeHandler : INotificationHandler<DomainEventNotification<ProductCreatedEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<ProductCreatedBridgeHandler> _logger;

    public ProductCreatedBridgeHandler(
        IMesaEventPublisher mesaPublisher,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<ProductCreatedBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<ProductCreatedEvent> wrapper, CancellationToken ct)
    {
        var e = wrapper.DomainEvent;
        _logger.LogDebug(
            "[MESA Bridge] ProductCreated yakalandi: {SKU}", e.SKU);

        var mesaEvent = new MesaProductCreatedEvent(
            e.ProductId, e.SKU, e.Name,
            null, e.SalePrice, null,
            _tenantProvider.GetCurrentTenantId(),
            e.OccurredAt);

        await _mesaPublisher.PublishProductCreatedAsync(mesaEvent, ct).ConfigureAwait(false);
        _monitor.RecordPublish("product.created");
    }
}

public class LowStockBridgeHandler : INotificationHandler<DomainEventNotification<LowStockDetectedEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<LowStockBridgeHandler> _logger;

    public LowStockBridgeHandler(
        IMesaEventPublisher mesaPublisher,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<LowStockBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<LowStockDetectedEvent> wrapper, CancellationToken ct)
    {
        var e = wrapper.DomainEvent;
        _logger.LogDebug(
            "[MESA Bridge] LowStockDetected yakalandi: {SKU}, stok={Stock}",
            e.SKU, e.CurrentStock);

        var mesaEvent = new MesaStockLowEvent(
            e.ProductId, e.SKU,
            e.CurrentStock, e.MinimumStock,
            null,
            _tenantProvider.GetCurrentTenantId(),
            e.OccurredAt);

        await _mesaPublisher.PublishStockLowAsync(mesaEvent, ct).ConfigureAwait(false);
        _monitor.RecordPublish("stock.low");
    }
}

public class OrderPlacedBridgeHandler : INotificationHandler<DomainEventNotification<OrderPlacedEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<OrderPlacedBridgeHandler> _logger;

    public OrderPlacedBridgeHandler(
        IMesaEventPublisher mesaPublisher,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<OrderPlacedBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<OrderPlacedEvent> wrapper, CancellationToken ct)
    {
        var e = wrapper.DomainEvent;
        _logger.LogDebug(
            "[MESA Bridge] OrderPlaced yakalandi: {OrderId}", e.OrderId);

        var mesaEvent = new MesaOrderReceivedEvent(
            e.OrderId, "MesTech", e.OrderNumber,
            e.TotalAmount, null,
            _tenantProvider.GetCurrentTenantId(),
            e.OccurredAt);

        await _mesaPublisher.PublishOrderReceivedAsync(mesaEvent, ct).ConfigureAwait(false);
        _monitor.RecordPublish("order.placed");
    }
}

public class PriceChangedBridgeHandler : INotificationHandler<DomainEventNotification<PriceChangedEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<PriceChangedBridgeHandler> _logger;

    public PriceChangedBridgeHandler(
        IMesaEventPublisher mesaPublisher,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<PriceChangedBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<PriceChangedEvent> wrapper, CancellationToken ct)
    {
        var e = wrapper.DomainEvent;
        _logger.LogDebug(
            "[MESA Bridge] PriceChanged yakalandi: {SKU}", e.SKU);

        var mesaEvent = new MesaPriceChangedEvent(
            e.ProductId, e.SKU,
            e.OldPrice, e.NewPrice,
            _tenantProvider.GetCurrentTenantId(),
            e.OccurredAt);

        await _mesaPublisher.PublishPriceChangedAsync(mesaEvent, ct).ConfigureAwait(false);
        _monitor.RecordPublish("price.changed");
    }
}

public class InvoiceGeneratedBridgeHandler : INotificationHandler<DomainEventNotification<InvoiceSentEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<InvoiceGeneratedBridgeHandler> _logger;

    public InvoiceGeneratedBridgeHandler(
        IMesaEventPublisher mesaPublisher,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<InvoiceGeneratedBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<InvoiceSentEvent> wrapper, CancellationToken ct)
    {
        var e = wrapper.DomainEvent;
        _logger.LogDebug(
            "[MESA Bridge] InvoiceSent yakalandi: InvoiceId={InvoiceId}", e.InvoiceId);

        var mesaEvent = new MesaInvoiceGeneratedEvent(
            e.InvoiceId, Guid.Empty, e.GibInvoiceId ?? string.Empty,
            "EFatura", null, null, null, 0m, e.PdfUrl,
            _tenantProvider.GetCurrentTenantId(),
            e.OccurredAt);

        await _mesaPublisher.PublishInvoiceGeneratedAsync(mesaEvent, ct).ConfigureAwait(false);
        _monitor.RecordPublish("invoice.generated");
    }
}

public class InvoiceCancelledBridgeHandler : INotificationHandler<DomainEventNotification<InvoiceCancelledEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<InvoiceCancelledBridgeHandler> _logger;

    public InvoiceCancelledBridgeHandler(
        IMesaEventPublisher mesaPublisher,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<InvoiceCancelledBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<InvoiceCancelledEvent> wrapper, CancellationToken ct)
    {
        var e = wrapper.DomainEvent;
        _logger.LogDebug(
            "[MESA Bridge] InvoiceCancelled yakalandi: {InvoiceNumber}", e.InvoiceNumber);

        var mesaEvent = new MesaInvoiceCancelledEvent(
            e.InvoiceId, e.InvoiceNumber, e.Reason,
            _tenantProvider.GetCurrentTenantId(),
            e.OccurredAt);

        await _mesaPublisher.PublishInvoiceCancelledAsync(mesaEvent, ct).ConfigureAwait(false);
        _monitor.RecordPublish("invoice.cancelled");
    }
}

public class ReturnCreatedBridgeHandler : INotificationHandler<DomainEventNotification<ReturnCreatedEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<ReturnCreatedBridgeHandler> _logger;

    public ReturnCreatedBridgeHandler(
        IMesaEventPublisher mesaPublisher,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<ReturnCreatedBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<ReturnCreatedEvent> wrapper, CancellationToken ct)
    {
        var e = wrapper.DomainEvent;
        _logger.LogDebug(
            "[MESA Bridge] ReturnCreated yakalandi: ReturnId={ReturnId}", e.ReturnRequestId);

        var mesaEvent = new MesaReturnCreatedEvent(
            e.ReturnRequestId, e.OrderId,
            e.Platform.ToString(), null, null,
            e.Reason.ToString(), 0, 0m,
            _tenantProvider.GetCurrentTenantId(),
            e.OccurredAt);

        await _mesaPublisher.PublishReturnCreatedAsync(mesaEvent, ct).ConfigureAwait(false);
        _monitor.RecordPublish("return.created");
    }
}

public class ReturnResolvedBridgeHandler : INotificationHandler<DomainEventNotification<ReturnResolvedEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<ReturnResolvedBridgeHandler> _logger;

    public ReturnResolvedBridgeHandler(
        IMesaEventPublisher mesaPublisher,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<ReturnResolvedBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<ReturnResolvedEvent> wrapper, CancellationToken ct)
    {
        var e = wrapper.DomainEvent;
        _logger.LogDebug(
            "[MESA Bridge] ReturnResolved yakalandi: ReturnId={ReturnId}", e.ReturnRequestId);

        var mesaEvent = new MesaReturnResolvedEvent(
            e.ReturnRequestId, e.OrderId,
            e.FinalStatus.ToString(), e.RefundAmount,
            _tenantProvider.GetCurrentTenantId(),
            e.OccurredAt);

        await _mesaPublisher.PublishReturnResolvedAsync(mesaEvent, ct).ConfigureAwait(false);
        _monitor.RecordPublish("return.resolved");
    }
}

public class BuyboxLostBridgeHandler : INotificationHandler<DomainEventNotification<BuyboxLostEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<BuyboxLostBridgeHandler> _logger;

    public BuyboxLostBridgeHandler(
        IMesaEventPublisher mesaPublisher,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<BuyboxLostBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<BuyboxLostEvent> wrapper, CancellationToken ct)
    {
        var e = wrapper.DomainEvent;
        _logger.LogDebug(
            "[MESA Bridge] BuyboxLost yakalandi: {SKU}, rakip={Competitor}",
            e.SKU, e.CompetitorName);

        var mesaEvent = new MesaBuyboxLostEvent(
            e.ProductId, e.SKU,
            e.CurrentPrice, e.CompetitorPrice, e.CompetitorName,
            e.CurrentPrice - e.CompetitorPrice,
            _tenantProvider.GetCurrentTenantId(),
            e.OccurredAt);

        await _mesaPublisher.PublishBuyboxLostAsync(mesaEvent, ct).ConfigureAwait(false);
        _monitor.RecordPublish("buybox.lost");
    }
}

public class SupplierFeedSyncedBridgeHandler : INotificationHandler<DomainEventNotification<SupplierFeedSyncedEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SupplierFeedSyncedBridgeHandler> _logger;

    public SupplierFeedSyncedBridgeHandler(
        IMesaEventPublisher mesaPublisher,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<SupplierFeedSyncedBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<SupplierFeedSyncedEvent> wrapper, CancellationToken ct)
    {
        var e = wrapper.DomainEvent;
        _logger.LogDebug(
            "[MESA Bridge] SupplierFeedSynced yakalandi: SupplierId={SupplierId}", e.SupplierId);

        var mesaEvent = new MesaSupplierFeedSyncedEvent(
            e.SupplierId, string.Empty, string.Empty,
            e.TotalProducts, 0, e.UpdatedProducts, e.DeactivatedProducts,
            _tenantProvider.GetCurrentTenantId(),
            e.OccurredAt);

        await _mesaPublisher.PublishSupplierFeedSyncedAsync(mesaEvent, ct).ConfigureAwait(false);
        _monitor.RecordPublish("supplier.feed.synced");
    }
}

public class DailySummaryBridgeHandler : INotificationHandler<DomainEventNotification<DailySummaryGeneratedEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<DailySummaryBridgeHandler> _logger;

    public DailySummaryBridgeHandler(
        IMesaEventPublisher mesaPublisher,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<DailySummaryBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<DailySummaryGeneratedEvent> wrapper, CancellationToken ct)
    {
        var e = wrapper.DomainEvent;
        _logger.LogDebug(
            "[MESA Bridge] DailySummary yakalandi: {Date}", e.Date);

        var mesaEvent = new MesaDailySummaryEvent(
            _tenantProvider.GetCurrentTenantId(),
            e.Date, e.OrderCount, e.Revenue, e.StockAlerts, e.InvoiceCount,
            e.OccurredAt);

        await _mesaPublisher.PublishDailySummaryAsync(mesaEvent, ct).ConfigureAwait(false);
        _monitor.RecordPublish("daily.summary");
    }
}

public class SyncErrorBridgeHandler : INotificationHandler<DomainEventNotification<SyncErrorOccurredEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SyncErrorBridgeHandler> _logger;

    public SyncErrorBridgeHandler(
        IMesaEventPublisher mesaPublisher,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<SyncErrorBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<SyncErrorOccurredEvent> wrapper, CancellationToken ct)
    {
        var e = wrapper.DomainEvent;
        _logger.LogDebug(
            "[MESA Bridge] SyncError yakalandi: {Platform} — {ErrorType}", e.Platform, e.ErrorType);

        var mesaEvent = new MesaSyncErrorEvent(
            _tenantProvider.GetCurrentTenantId(),
            e.Platform, e.ErrorType, e.Message,
            e.OccurredAt);

        await _mesaPublisher.PublishSyncErrorAsync(mesaEvent, ct).ConfigureAwait(false);
        _monitor.RecordPublish("sync.error");
    }
}
