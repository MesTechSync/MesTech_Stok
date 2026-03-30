using MassTransit;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishStockChangedAsync(Guid productId, string sku, int newQuantity, string source, CancellationToken ct = default);
    Task PublishPriceChangedAsync(Guid productId, string sku, decimal newPrice, string source, CancellationToken ct = default);
    Task PublishOrderReceivedAsync(Guid orderId, string platformCode, string platformOrderId, decimal totalAmount, CancellationToken ct = default);
    Task PublishInvoiceCreatedAsync(Guid invoiceId, Guid orderId, string invoiceNumber, decimal grandTotal, CancellationToken ct = default);
    Task PublishOrderShippedAsync(Guid orderId, string trackingNumber, string cargoProvider, CancellationToken ct = default);
    Task PublishProductUpdatedAsync(Guid productId, string sku, string updatedField, CancellationToken ct = default);
    Task PublishShipmentCostRecordedAsync(Guid orderId, string trackingNumber, string cargoProvider, decimal shippingCost, CancellationToken ct = default);
    Task PublishZeroStockDetectedAsync(Guid productId, string sku, int previousStock, CancellationToken ct = default);
    Task PublishStaleOrderDetectedAsync(Guid orderId, string orderNumber, string? platformCode, double hoursElapsed, CancellationToken ct = default);
    Task PublishPlatformDeactivatedAsync(Guid productId, string sku, string platformCode, CancellationToken ct = default);
    Task PublishEInvoiceSentAsync(Guid invoiceId, string ettnNo, string providerId, decimal totalAmount, string currency, CancellationToken ct = default);
    Task PublishEInvoiceCancelledAsync(Guid invoiceId, string ettnNo, string reason, CancellationToken ct = default);
    Task PublishErpSyncCompletedAsync(string erpProvider, string entityType, Guid entityId, string? erpRef, bool success, CancellationToken ct = default);
    Task PublishEbayOrderReceivedAsync(string ebayOrderId, string buyerUsername, decimal totalAmount, string currency, CancellationToken ct = default);
    Task PublishCreditBalanceLowAsync(string providerId, int remainingCredits, int thresholdCredits, CancellationToken ct = default);
}

public sealed class IntegrationEventPublisher : IIntegrationEventPublisher
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

    public async Task PublishStockChangedAsync(Guid productId, string sku, int newQuantity, string source, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new StockChangedIntegrationEvent(productId, sku, newQuantity, source, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt, ct).ConfigureAwait(false);
        _logger.LogInformation("StockChanged yayinlandi: {SKU} -> {Qty} ({Source}) [Tenant={TenantId}]", sku, newQuantity, source, tenantId);
    }

    public async Task PublishPriceChangedAsync(Guid productId, string sku, decimal newPrice, string source, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new PriceChangedIntegrationEvent(productId, sku, newPrice, source, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt, ct).ConfigureAwait(false);
        _logger.LogInformation("PriceChanged yayinlandi: {SKU} -> {Price} ({Source}) [Tenant={TenantId}]", sku, newPrice, source, tenantId);
    }

    public async Task PublishOrderReceivedAsync(Guid orderId, string platformCode, string platformOrderId, decimal totalAmount, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new OrderReceivedIntegrationEvent(orderId, platformCode, platformOrderId, totalAmount, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt, ct).ConfigureAwait(false);
        _logger.LogInformation("OrderReceived yayinlandi: {Platform} #{PlatformOrderId} [Tenant={TenantId}]", platformCode, platformOrderId, tenantId);
    }

    public async Task PublishInvoiceCreatedAsync(Guid invoiceId, Guid orderId, string invoiceNumber, decimal grandTotal, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new InvoiceCreatedIntegrationEvent(invoiceId, orderId, invoiceNumber, grandTotal, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt, ct).ConfigureAwait(false);
        _logger.LogInformation("InvoiceCreated yayinlandi: {InvoiceNumber} -> {Total} [Tenant={TenantId}]", invoiceNumber, grandTotal, tenantId);
    }

    public async Task PublishOrderShippedAsync(Guid orderId, string trackingNumber, string cargoProvider, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new OrderShippedIntegrationEvent(orderId, trackingNumber, cargoProvider, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt, ct).ConfigureAwait(false);
        _logger.LogInformation("OrderShipped yayinlandi: {OrderId} kargo={Cargo} takip={Tracking} [Tenant={TenantId}]",
            orderId, cargoProvider, trackingNumber, tenantId);
    }

    public async Task PublishProductUpdatedAsync(Guid productId, string sku, string updatedField, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new ProductUpdatedIntegrationEvent(productId, sku, updatedField, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt, ct).ConfigureAwait(false);
        _logger.LogInformation("ProductUpdated yayinlandi: {SKU} alan={Field} [Tenant={TenantId}]",
            sku, updatedField, tenantId);
    }

    public async Task PublishShipmentCostRecordedAsync(Guid orderId, string trackingNumber, string cargoProvider, decimal shippingCost, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new ShipmentCostRecordedIntegrationEvent(orderId, trackingNumber, cargoProvider, shippingCost, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt, ct).ConfigureAwait(false);
        _logger.LogInformation("ShipmentCostRecorded yayinlandi: {OrderId} kargo={Cargo} maliyet={Cost} [Tenant={TenantId}]",
            orderId, cargoProvider, shippingCost, tenantId);
    }

    public async Task PublishZeroStockDetectedAsync(Guid productId, string sku, int previousStock, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new ZeroStockIntegrationEvent(productId, sku, previousStock, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt, ct).ConfigureAwait(false);
        _logger.LogInformation("ZeroStockDetected yayinlandi: {SKU} onceki={PrevStock} [Tenant={TenantId}]",
            sku, previousStock, tenantId);
    }

    public async Task PublishStaleOrderDetectedAsync(Guid orderId, string orderNumber, string? platformCode, double hoursElapsed, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new StaleOrderDetectedIntegrationEvent(orderId, orderNumber, platformCode, hoursElapsed, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt, ct).ConfigureAwait(false);
        _logger.LogInformation("StaleOrderDetected yayinlandi: {OrderNumber} platform={Platform} saat={Hours} [Tenant={TenantId}]",
            orderNumber, platformCode, hoursElapsed, tenantId);
    }

    public async Task PublishPlatformDeactivatedAsync(Guid productId, string sku, string platformCode, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new PlatformDeactivatedIntegrationEvent(productId, sku, platformCode, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt, ct).ConfigureAwait(false);
        _logger.LogInformation("PlatformDeactivated yayinlandi: {SKU} platform={Platform} [Tenant={TenantId}]",
            sku, platformCode, tenantId);
    }

    public async Task PublishEInvoiceSentAsync(Guid invoiceId, string ettnNo, string providerId, decimal totalAmount, string currency, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new EInvoiceSentIntegrationEvent(invoiceId, ettnNo, providerId, totalAmount, currency, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt, ct).ConfigureAwait(false);
        _logger.LogInformation("EInvoiceSent yayinlandi: ETTN={Ettn} provider={Provider} tutar={Amount} [Tenant={TenantId}]",
            ettnNo, providerId, totalAmount, tenantId);
    }

    public async Task PublishEInvoiceCancelledAsync(Guid invoiceId, string ettnNo, string reason, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new EInvoiceCancelledIntegrationEvent(invoiceId, ettnNo, reason, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt, ct).ConfigureAwait(false);
        _logger.LogInformation("EInvoiceCancelled yayinlandi: ETTN={Ettn} sebep={Reason} [Tenant={TenantId}]",
            ettnNo, reason, tenantId);
    }

    public async Task PublishErpSyncCompletedAsync(string erpProvider, string entityType, Guid entityId, string? erpRef, bool success, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new ErpSyncCompletedIntegrationEvent(erpProvider, entityType, entityId, erpRef, success, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt, ct).ConfigureAwait(false);
        _logger.LogInformation("ErpSyncCompleted yayinlandi: {ErpProvider} {EntityType} basari={Success} [Tenant={TenantId}]",
            erpProvider, entityType, success, tenantId);
    }

    public async Task PublishEbayOrderReceivedAsync(string ebayOrderId, string buyerUsername, decimal totalAmount, string currency, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new EbayOrderReceivedIntegrationEvent(ebayOrderId, buyerUsername, totalAmount, currency, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt, ct).ConfigureAwait(false);
        _logger.LogInformation("EbayOrderReceived yayinlandi: {EbayOrderId} alici={Buyer} tutar={Amount} [Tenant={TenantId}]",
            ebayOrderId, buyerUsername, totalAmount, tenantId);
    }

    public async Task PublishCreditBalanceLowAsync(string providerId, int remainingCredits, int thresholdCredits, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var evt = new CreditBalanceLowIntegrationEvent(providerId, remainingCredits, thresholdCredits, tenantId, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt, ct).ConfigureAwait(false);
        _logger.LogWarning("CreditBalanceLow yayinlandi: provider={Provider} kalan={Remaining}/{Threshold} [Tenant={TenantId}]",
            providerId, remainingCredits, thresholdCredits, tenantId);
    }
}
