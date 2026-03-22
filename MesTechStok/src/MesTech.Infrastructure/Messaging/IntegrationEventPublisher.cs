using MassTransit;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishStockChangedAsync(Guid productId, string sku, int newQuantity, string source);
    Task PublishPriceChangedAsync(Guid productId, string sku, decimal newPrice, string source);
    Task PublishOrderReceivedAsync(Guid orderId, string platformCode, string platformOrderId, decimal totalAmount);
    Task PublishInvoiceCreatedAsync(Guid invoiceId, Guid orderId, string invoiceNumber, decimal grandTotal);
    Task PublishOrderShippedAsync(Guid orderId, string trackingNumber, string cargoProvider);
    Task PublishProductUpdatedAsync(Guid productId, string sku, string updatedField);
}

public class IntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<IntegrationEventPublisher> _logger;

    public IntegrationEventPublisher(
        IPublishEndpoint publishEndpoint,
        ITenantProvider tenantProvider,
        ILogger<IntegrationEventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task PublishStockChangedAsync(Guid productId, string sku, int newQuantity, string source)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new StockChangedIntegrationEvent(productId, sku, newQuantity, source, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt).ConfigureAwait(false);
        _logger.LogInformation("StockChanged yayinlandi: {SKU} -> {Qty} ({Source}) [Tenant={TenantId}]", sku, newQuantity, source, tenantId);
    }

    public async Task PublishPriceChangedAsync(Guid productId, string sku, decimal newPrice, string source)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new PriceChangedIntegrationEvent(productId, sku, newPrice, source, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt).ConfigureAwait(false);
        _logger.LogInformation("PriceChanged yayinlandi: {SKU} -> {Price} ({Source}) [Tenant={TenantId}]", sku, newPrice, source, tenantId);
    }

    public async Task PublishOrderReceivedAsync(Guid orderId, string platformCode, string platformOrderId, decimal totalAmount)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new OrderReceivedIntegrationEvent(orderId, platformCode, platformOrderId, totalAmount, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt).ConfigureAwait(false);
        _logger.LogInformation("OrderReceived yayinlandi: {Platform} #{PlatformOrderId} [Tenant={TenantId}]", platformCode, platformOrderId, tenantId);
    }

    public async Task PublishInvoiceCreatedAsync(Guid invoiceId, Guid orderId, string invoiceNumber, decimal grandTotal)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new InvoiceCreatedIntegrationEvent(invoiceId, orderId, invoiceNumber, grandTotal, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt).ConfigureAwait(false);
        _logger.LogInformation("InvoiceCreated yayinlandi: {InvoiceNumber} -> {Total} [Tenant={TenantId}]", invoiceNumber, grandTotal, tenantId);
    }

    public async Task PublishOrderShippedAsync(Guid orderId, string trackingNumber, string cargoProvider)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new OrderShippedIntegrationEvent(orderId, trackingNumber, cargoProvider, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt).ConfigureAwait(false);
        _logger.LogInformation("OrderShipped yayinlandi: {OrderId} kargo={Cargo} takip={Tracking} [Tenant={TenantId}]",
            orderId, cargoProvider, trackingNumber, tenantId);
    }

    public async Task PublishProductUpdatedAsync(Guid productId, string sku, string updatedField)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new ProductUpdatedIntegrationEvent(productId, sku, updatedField, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt).ConfigureAwait(false);
        _logger.LogInformation("ProductUpdated yayinlandi: {SKU} alan={Field} [Tenant={TenantId}]",
            sku, updatedField, tenantId);
    }
}
