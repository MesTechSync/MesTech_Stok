using MassTransit;
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
    private readonly ILogger<IntegrationEventPublisher> _logger;

    public IntegrationEventPublisher(
        IPublishEndpoint publishEndpoint,
        ILogger<IntegrationEventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishStockChangedAsync(Guid productId, string sku, int newQuantity, string source)
    {
        var evt = new StockChangedIntegrationEvent(productId, sku, newQuantity, source, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt);
        _logger.LogInformation("StockChanged yayinlandi: {SKU} -> {Qty} ({Source})", sku, newQuantity, source);
    }

    public async Task PublishPriceChangedAsync(Guid productId, string sku, decimal newPrice, string source)
    {
        var evt = new PriceChangedIntegrationEvent(productId, sku, newPrice, source, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt);
        _logger.LogInformation("PriceChanged yayinlandi: {SKU} -> {Price} ({Source})", sku, newPrice, source);
    }

    public async Task PublishOrderReceivedAsync(Guid orderId, string platformCode, string platformOrderId, decimal totalAmount)
    {
        var evt = new OrderReceivedIntegrationEvent(orderId, platformCode, platformOrderId, totalAmount, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt);
        _logger.LogInformation("OrderReceived yayinlandi: {Platform} #{PlatformOrderId}", platformCode, platformOrderId);
    }

    public async Task PublishInvoiceCreatedAsync(Guid invoiceId, Guid orderId, string invoiceNumber, decimal grandTotal)
    {
        var evt = new InvoiceCreatedIntegrationEvent(invoiceId, orderId, invoiceNumber, grandTotal, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt);
        _logger.LogInformation("InvoiceCreated yayinlandi: {InvoiceNumber} -> {Total}", invoiceNumber, grandTotal);
    }
}
