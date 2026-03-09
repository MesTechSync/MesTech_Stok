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
        await _publishEndpoint.Publish(evt);
        _logger.LogInformation("StockChanged yayinlandi: {SKU} -> {Qty} ({Source}) [Tenant={TenantId}]", sku, newQuantity, source, tenantId);
    }

    public async Task PublishPriceChangedAsync(Guid productId, string sku, decimal newPrice, string source)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new PriceChangedIntegrationEvent(productId, sku, newPrice, source, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt);
        _logger.LogInformation("PriceChanged yayinlandi: {SKU} -> {Price} ({Source}) [Tenant={TenantId}]", sku, newPrice, source, tenantId);
    }

    public async Task PublishOrderReceivedAsync(Guid orderId, string platformCode, string platformOrderId, decimal totalAmount)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new OrderReceivedIntegrationEvent(orderId, platformCode, platformOrderId, totalAmount, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt);
        _logger.LogInformation("OrderReceived yayinlandi: {Platform} #{PlatformOrderId} [Tenant={TenantId}]", platformCode, platformOrderId, tenantId);
    }

    public async Task PublishInvoiceCreatedAsync(Guid invoiceId, Guid orderId, string invoiceNumber, decimal grandTotal)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new InvoiceCreatedIntegrationEvent(invoiceId, orderId, invoiceNumber, grandTotal, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt);
        _logger.LogInformation("InvoiceCreated yayinlandi: {InvoiceNumber} -> {Total} [Tenant={TenantId}]", invoiceNumber, grandTotal, tenantId);
    }
}
