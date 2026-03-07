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

        await _mesaPublisher.PublishProductCreatedAsync(mesaEvent, ct);
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

        await _mesaPublisher.PublishStockLowAsync(mesaEvent, ct);
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

        await _mesaPublisher.PublishOrderReceivedAsync(mesaEvent, ct);
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

        await _mesaPublisher.PublishPriceChangedAsync(mesaEvent, ct);
        _monitor.RecordPublish("price.changed");
    }
}
