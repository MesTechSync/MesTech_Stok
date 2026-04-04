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
public sealed class ProductCreatedBridgeHandler : INotificationHandler<DomainEventNotification<ProductCreatedEvent>>
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
        // RecordPublish now called inside MesaEventPublisher — no double-count
    }
}

public sealed class LowStockBridgeHandler : INotificationHandler<DomainEventNotification<LowStockDetectedEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<LowStockBridgeHandler> _logger;

    private readonly IDashboardNotifier _notifier;

    public LowStockBridgeHandler(
        IMesaEventPublisher mesaPublisher,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        IDashboardNotifier notifier,
        ILogger<LowStockBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _notifier = notifier;
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
        // NOTE: WebSocket push handled by SignalRNotificationBridge (same event, avoid duplicate broadcast)
    }
}

public sealed class OrderPlacedBridgeHandler : INotificationHandler<DomainEventNotification<OrderPlacedEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<OrderPlacedBridgeHandler> _logger;

    private readonly IDashboardNotifier _notifier;

    public OrderPlacedBridgeHandler(
        IMesaEventPublisher mesaPublisher,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        IDashboardNotifier notifier,
        ILogger<OrderPlacedBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _notifier = notifier;
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
        // WPF realtime WebSocket push
        await _notifier.NotifyNewOrderAsync("MesTech", e.OrderNumber, e.TotalAmount, 0, ct).ConfigureAwait(false);
    }
}

public sealed class PriceChangedBridgeHandler : INotificationHandler<DomainEventNotification<PriceChangedEvent>>
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
        // RecordPublish inside MesaEventPublisher
    }
}

public sealed class InvoiceGeneratedBridgeHandler : INotificationHandler<DomainEventNotification<InvoiceSentEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<InvoiceGeneratedBridgeHandler> _logger;

    private readonly IDashboardNotifier _notifier;

    public InvoiceGeneratedBridgeHandler(
        IMesaEventPublisher mesaPublisher,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        IDashboardNotifier notifier,
        ILogger<InvoiceGeneratedBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<InvoiceSentEvent> wrapper, CancellationToken ct)
    {
        var e = wrapper.DomainEvent;
        _logger.LogDebug(
            "[MESA Bridge] InvoiceSent yakalandi: InvoiceId={InvoiceId}", e.InvoiceId);

        var mesaEvent = new MesaInvoiceGeneratedEvent(
            e.InvoiceId, e.OrderId, e.GibInvoiceId ?? string.Empty,
            "EFatura", null, null, null, 0m, e.PdfUrl,
            _tenantProvider.GetCurrentTenantId(),
            e.OccurredAt);

        await _mesaPublisher.PublishInvoiceGeneratedAsync(mesaEvent, ct).ConfigureAwait(false);
        // WPF realtime WebSocket push
        await _notifier.NotifyInvoiceGeneratedAsync(
            e.GibInvoiceId ?? e.InvoiceId.ToString(), string.Empty, 0m, ct).ConfigureAwait(false);
    }
}

public sealed class InvoiceCancelledBridgeHandler : INotificationHandler<DomainEventNotification<InvoiceCancelledEvent>>
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
        // RecordPublish inside MesaEventPublisher
    }
}

public sealed class ReturnCreatedBridgeHandler : INotificationHandler<DomainEventNotification<ReturnCreatedEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<ReturnCreatedBridgeHandler> _logger;

    private readonly IDashboardNotifier _notifier;

    public ReturnCreatedBridgeHandler(
        IMesaEventPublisher mesaPublisher,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        IDashboardNotifier notifier,
        ILogger<ReturnCreatedBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _notifier = notifier;
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
        // WPF realtime WebSocket push
        await _notifier.NotifyReturnCreatedAsync(
            e.Platform.ToString(), e.ReturnRequestId.ToString(),
            e.OrderId.ToString(), e.Reason.ToString(), ct).ConfigureAwait(false);
    }
}

public sealed class ReturnResolvedBridgeHandler : INotificationHandler<DomainEventNotification<ReturnResolvedEvent>>
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
        // RecordPublish inside MesaEventPublisher
    }
}

public sealed class BuyboxLostBridgeHandler : INotificationHandler<DomainEventNotification<BuyboxLostEvent>>
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
        // RecordPublish inside MesaEventPublisher
    }
}

public sealed class SupplierFeedSyncedBridgeHandler : INotificationHandler<DomainEventNotification<SupplierFeedSyncedEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SupplierFeedSyncedBridgeHandler> _logger;

    private readonly IDashboardNotifier _notifier;

    public SupplierFeedSyncedBridgeHandler(
        IMesaEventPublisher mesaPublisher,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        IDashboardNotifier notifier,
        ILogger<SupplierFeedSyncedBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _notifier = notifier;
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
        // WPF realtime sync status push
        await _notifier.NotifySyncStatusAsync(
            "SupplierFeed", "completed", e.TotalProducts, e.TotalProducts, ct).ConfigureAwait(false);
    }
}

public sealed class DailySummaryBridgeHandler : INotificationHandler<DomainEventNotification<DailySummaryGeneratedEvent>>
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
        // RecordPublish inside MesaEventPublisher
    }
}

public sealed class SyncErrorBridgeHandler : INotificationHandler<DomainEventNotification<SyncErrorOccurredEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SyncErrorBridgeHandler> _logger;

    private readonly IDashboardNotifier _notifier;

    public SyncErrorBridgeHandler(
        IMesaEventPublisher mesaPublisher,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        IDashboardNotifier notifier,
        ILogger<SyncErrorBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _notifier = notifier;
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
        // NOTE: WebSocket push handled by SignalRNotificationBridge (same event, avoid duplicate broadcast)
    }
}

// ── V4 Bridge Handlers ──────────────────────────────────────────

public sealed class ZeroStockBridgeHandler : INotificationHandler<DomainEventNotification<ZeroStockDetectedEvent>>
{
    private readonly ILogger<ZeroStockBridgeHandler> _logger;
    public ZeroStockBridgeHandler(ILogger<ZeroStockBridgeHandler> logger) => _logger = logger;

    public Task Handle(DomainEventNotification<ZeroStockDetectedEvent> wrapper, CancellationToken ct)
    {
        var e = wrapper.DomainEvent;
        _logger.LogWarning("[MESA Bridge] ZeroStock: SKU={SKU}, Previous={Prev}", e.SKU, e.PreviousStock);
        return Task.CompletedTask;
    }
}

public sealed class PriceLossBridgeHandler : INotificationHandler<DomainEventNotification<PriceLossDetectedEvent>>
{
    private readonly ILogger<PriceLossBridgeHandler> _logger;
    public PriceLossBridgeHandler(ILogger<PriceLossBridgeHandler> logger) => _logger = logger;

    public Task Handle(DomainEventNotification<PriceLossDetectedEvent> wrapper, CancellationToken ct)
    {
        var e = wrapper.DomainEvent;
        _logger.LogWarning("[MESA Bridge] PriceLoss: SKU={SKU}, Loss={Loss}/unit", e.SKU, e.LossPerUnit);
        return Task.CompletedTask;
    }
}

public sealed class StaleOrderBridgeHandler : INotificationHandler<DomainEventNotification<StaleOrderDetectedEvent>>
{
    private readonly ILogger<StaleOrderBridgeHandler> _logger;
    public StaleOrderBridgeHandler(ILogger<StaleOrderBridgeHandler> logger) => _logger = logger;

    public Task Handle(DomainEventNotification<StaleOrderDetectedEvent> wrapper, CancellationToken ct)
    {
        var e = wrapper.DomainEvent;
        _logger.LogWarning("[MESA Bridge] StaleOrder: {OrderNumber}, Elapsed={Hours}h", e.OrderNumber, e.ElapsedSince.TotalHours);
        return Task.CompletedTask;
    }
}

public sealed class ReturnApprovedBridgeHandler : INotificationHandler<DomainEventNotification<ReturnApprovedEvent>>
{
    private readonly ILogger<ReturnApprovedBridgeHandler> _logger;
    public ReturnApprovedBridgeHandler(ILogger<ReturnApprovedBridgeHandler> logger) => _logger = logger;

    public Task Handle(DomainEventNotification<ReturnApprovedEvent> wrapper, CancellationToken ct)
    {
        var e = wrapper.DomainEvent;
        _logger.LogInformation("[MESA Bridge] ReturnApproved: ReturnId={ReturnId}, Lines={Count}", e.ReturnRequestId, e.Lines.Count);
        return Task.CompletedTask;
    }
}
