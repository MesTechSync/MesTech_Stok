using MassTransit;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa;

/// <summary>
/// MESA OS'a yonelik integration event'leri RabbitMQ'ya publish eder.
/// Mevcut IntegrationEventPublisher'a dokunmaz — ayri publisher.
/// </summary>
public interface IMesaEventPublisher
{
    Task PublishProductCreatedAsync(MesaProductCreatedEvent evt, CancellationToken ct = default);
    Task PublishStockLowAsync(MesaStockLowEvent evt, CancellationToken ct = default);
    Task PublishOrderReceivedAsync(MesaOrderReceivedEvent evt, CancellationToken ct = default);
    Task PublishPriceChangedAsync(MesaPriceChangedEvent evt, CancellationToken ct = default);
}

public class MesaEventPublisher : IMesaEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<MesaEventPublisher> _logger;

    public MesaEventPublisher(
        IPublishEndpoint publishEndpoint,
        ILogger<MesaEventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishProductCreatedAsync(
        MesaProductCreatedEvent evt, CancellationToken ct = default)
    {
        await _publishEndpoint.Publish(evt, ct);
        _logger.LogInformation(
            "[MESA] ProductCreated yayinlandi: {SKU} - {Name} (Tenant: {TenantId})",
            evt.SKU, evt.Name, evt.TenantId);
    }

    public async Task PublishStockLowAsync(
        MesaStockLowEvent evt, CancellationToken ct = default)
    {
        await _publishEndpoint.Publish(evt, ct);
        _logger.LogWarning(
            "[MESA] StockLow yayinlandi: {SKU} stok={Current}, min={Min} (Tenant: {TenantId})",
            evt.SKU, evt.CurrentStock, evt.MinimumStock, evt.TenantId);
    }

    public async Task PublishOrderReceivedAsync(
        MesaOrderReceivedEvent evt, CancellationToken ct = default)
    {
        await _publishEndpoint.Publish(evt, ct);
        _logger.LogInformation(
            "[MESA] OrderReceived yayinlandi: {Platform} #{OrderId}, tutar={Amount} (Tenant: {TenantId})",
            evt.PlatformCode, evt.PlatformOrderId, evt.TotalAmount, evt.TenantId);
    }

    public async Task PublishPriceChangedAsync(
        MesaPriceChangedEvent evt, CancellationToken ct = default)
    {
        await _publishEndpoint.Publish(evt, ct);
        _logger.LogInformation(
            "[MESA] PriceChanged yayinlandi: {SKU} {Old} -> {New} (Tenant: {TenantId})",
            evt.SKU, evt.OldPrice, evt.NewPrice, evt.TenantId);
    }
}
