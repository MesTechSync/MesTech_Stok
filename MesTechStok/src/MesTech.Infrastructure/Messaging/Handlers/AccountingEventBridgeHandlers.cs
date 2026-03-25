using MediatR;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Handlers;

// ════════════════════════════════════════════════════════════════════════════
// ACCOUNTING EVENT BRIDGE HANDLERS
// ════════════════════════════════════════════════════════════════════════════
//
// Muhasebe GL handler'ları — event property'lerinde eksik veri var,
// repository'den zenginleştirme (enrichment) gerekli.
//
// Z3: InvoiceApproved → GL yevmiye (taxAmount, netAmount repo'dan)
// Z4: InvoiceCancelled → GL ters yevmiye (grandTotal repo'dan)
// Z7b: OrderShipped → Kargo maliyet GL (shippingCost repo'dan)
//
// CommissionChargedGLHandler: PARKED — CommissionChargedEvent yok.
// Komisyon GL kaydı şu an Hangfire job'ından tetiklenmeli.
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Zincir 3: InvoiceApprovedEvent → GL yevmiye kaydı.
/// Event'te TaxAmount/NetAmount yok — Invoice repo'dan çekilir.
/// </summary>
public sealed class InvoiceApprovedGLBridge
    : INotificationHandler<DomainEventNotification<InvoiceApprovedEvent>>
{
    private readonly IInvoiceApprovedGLHandler _handler;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly ILogger<InvoiceApprovedGLBridge> _logger;

    public InvoiceApprovedGLBridge(
        IInvoiceApprovedGLHandler handler,
        IInvoiceRepository invoiceRepo,
        ILogger<InvoiceApprovedGLBridge> logger)
    {
        _handler = handler;
        _invoiceRepo = invoiceRepo;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<InvoiceApprovedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        var invoice = await _invoiceRepo.GetByIdAsync(e.InvoiceId).ConfigureAwait(false);
        if (invoice is null)
        {
            _logger.LogWarning(
                "[Bridge] InvoiceApproved → GL: Fatura bulunamadi, InvoiceId={InvoiceId}",
                e.InvoiceId);
            return;
        }

        await _handler.HandleAsync(
            e.InvoiceId, e.TenantId, e.InvoiceNumber,
            invoice.GrandTotal, invoice.TaxTotal, invoice.SubTotal,
            cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Zincir 4: InvoiceCancelledEvent → GL ters yevmiye.
/// Event'te GrandTotal yok — Invoice repo'dan çekilir.
/// </summary>
public sealed class InvoiceCancelledReversalBridge
    : INotificationHandler<DomainEventNotification<InvoiceCancelledEvent>>
{
    private readonly IInvoiceCancelledReversalHandler _handler;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly ILogger<InvoiceCancelledReversalBridge> _logger;

    public InvoiceCancelledReversalBridge(
        IInvoiceCancelledReversalHandler handler,
        IInvoiceRepository invoiceRepo,
        ILogger<InvoiceCancelledReversalBridge> logger)
    {
        _handler = handler;
        _invoiceRepo = invoiceRepo;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<InvoiceCancelledEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        var invoice = await _invoiceRepo.GetByIdAsync(e.InvoiceId).ConfigureAwait(false);
        if (invoice is null)
        {
            _logger.LogWarning(
                "[Bridge] InvoiceCancelled → Reversal: Fatura bulunamadi, InvoiceId={InvoiceId}",
                e.InvoiceId);
            return;
        }

        await _handler.HandleAsync(
            e.InvoiceId, e.OrderId, e.InvoiceNumber,
            e.Reason, e.TenantId, invoice.GrandTotal,
            cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Zincir 7b: OrderShippedEvent → Kargo maliyet GL kaydı.
/// Event'te ShippingCost yok — ShipmentCost repo'dan çekilir.
/// </summary>
public sealed class OrderShippedCostBridge
    : INotificationHandler<DomainEventNotification<OrderShippedEvent>>
{
    private readonly IOrderShippedCostHandler _handler;
    private readonly IShipmentCostRepository _shipmentCostRepo;
    private readonly ILogger<OrderShippedCostBridge> _logger;

    public OrderShippedCostBridge(
        IOrderShippedCostHandler handler,
        IShipmentCostRepository shipmentCostRepo,
        ILogger<OrderShippedCostBridge> logger)
    {
        _handler = handler;
        _shipmentCostRepo = shipmentCostRepo;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<OrderShippedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        var costs = await _shipmentCostRepo
            .GetByOrderIdAsync(e.OrderId, cancellationToken)
            .ConfigureAwait(false);

        var totalCost = costs.Sum(c => c.Cost);
        if (totalCost <= 0)
        {
            _logger.LogDebug(
                "[Bridge] OrderShipped → Cost GL: Kargo maliyeti 0, atlanıyor. OrderId={OrderId}",
                e.OrderId);
            return;
        }

        await _handler.HandleAsync(
            e.OrderId, e.TenantId, e.TrackingNumber,
            e.CargoProvider, totalCost,
            cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Zincir 6: CommissionChargedEvent → GL komisyon gider kaydı.
/// Event string Platform → PlatformType enum dönüşümü bridge'de yapılır.
/// </summary>
public sealed class CommissionChargedGLBridge
    : INotificationHandler<DomainEventNotification<CommissionChargedEvent>>
{
    private readonly ICommissionChargedGLHandler _handler;
    private readonly ILogger<CommissionChargedGLBridge> _logger;

    public CommissionChargedGLBridge(
        ICommissionChargedGLHandler handler,
        ILogger<CommissionChargedGLBridge> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<CommissionChargedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        // Platform string → PlatformType enum dönüşümü
        if (!Enum.TryParse<PlatformType>(e.Platform, ignoreCase: true, out var platformType))
            platformType = PlatformType.OpenCart; // fallback — bilinmeyen platform

        // OrderId string → Guid dönüşümü
        var orderId = Guid.TryParse(e.OrderId, out var parsedOrderId)
            ? parsedOrderId
            : Guid.Empty;

        _logger.LogDebug(
            "[Bridge] CommissionCharged → GL: Platform={Platform}, Amount={Amount}, Rate={Rate}%",
            platformType, e.CommissionAmount, e.CommissionRate * 100);

        await _handler.HandleAsync(
            orderId, e.TenantId, platformType,
            e.CommissionAmount, e.CommissionRate,
            cancellationToken).ConfigureAwait(false);
    }
}
