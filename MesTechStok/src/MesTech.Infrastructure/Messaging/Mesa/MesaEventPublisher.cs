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
    Task PublishInvoiceGeneratedAsync(MesaInvoiceGeneratedEvent evt, CancellationToken ct = default);
    Task PublishInvoiceCancelledAsync(MesaInvoiceCancelledEvent evt, CancellationToken ct = default);
    Task PublishReturnCreatedAsync(MesaReturnCreatedEvent evt, CancellationToken ct = default);
    Task PublishReturnResolvedAsync(MesaReturnResolvedEvent evt, CancellationToken ct = default);
    Task PublishBuyboxLostAsync(MesaBuyboxLostEvent evt, CancellationToken ct = default);
    Task PublishSupplierFeedSyncedAsync(MesaSupplierFeedSyncedEvent evt, CancellationToken ct = default);
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

    public async Task PublishInvoiceGeneratedAsync(
        MesaInvoiceGeneratedEvent evt, CancellationToken ct = default)
    {
        await _publishEndpoint.Publish(evt, ct);
        _logger.LogInformation(
            "[MESA] InvoiceGenerated yayinlandi: {InvoiceNumber}, tutar={Total} (Tenant: {TenantId})",
            evt.InvoiceNumber, evt.GrandTotal, evt.TenantId);
    }

    public async Task PublishInvoiceCancelledAsync(
        MesaInvoiceCancelledEvent evt, CancellationToken ct = default)
    {
        await _publishEndpoint.Publish(evt, ct);
        _logger.LogWarning(
            "[MESA] InvoiceCancelled yayinlandi: {InvoiceNumber}, sebep={Reason} (Tenant: {TenantId})",
            evt.InvoiceNumber, evt.CancelReason, evt.TenantId);
    }

    public async Task PublishReturnCreatedAsync(
        MesaReturnCreatedEvent evt, CancellationToken ct = default)
    {
        await _publishEndpoint.Publish(evt, ct);
        _logger.LogInformation(
            "[MESA] ReturnCreated yayinlandi: siparis={OrderId}, platform={Platform} (Tenant: {TenantId})",
            evt.OrderId, evt.PlatformCode, evt.TenantId);
    }

    public async Task PublishReturnResolvedAsync(
        MesaReturnResolvedEvent evt, CancellationToken ct = default)
    {
        await _publishEndpoint.Publish(evt, ct);
        _logger.LogInformation(
            "[MESA] ReturnResolved yayinlandi: iade={ReturnId}, sonuc={Resolution} (Tenant: {TenantId})",
            evt.ReturnRequestId, evt.Resolution, evt.TenantId);
    }

    public async Task PublishBuyboxLostAsync(
        MesaBuyboxLostEvent evt, CancellationToken ct = default)
    {
        await _publishEndpoint.Publish(evt, ct);
        _logger.LogWarning(
            "[MESA] BuyboxLost yayinlandi: {SKU}, rakip={Competitor} fiyat={CompPrice} (Tenant: {TenantId})",
            evt.SKU, evt.CompetitorName, evt.CompetitorPrice, evt.TenantId);
    }

    public async Task PublishSupplierFeedSyncedAsync(
        MesaSupplierFeedSyncedEvent evt, CancellationToken ct = default)
    {
        await _publishEndpoint.Publish(evt, ct);
        _logger.LogInformation(
            "[MESA] SupplierFeedSynced yayinlandi: {Supplier}, toplam={Total}, yeni={New} (Tenant: {TenantId})",
            evt.SupplierName, evt.ProductsTotal, evt.ProductsNew, evt.TenantId);
    }
}
