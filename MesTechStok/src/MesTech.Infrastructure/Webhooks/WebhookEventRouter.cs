using System.Text.Json;
using MediatR;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Webhooks;

/// <summary>
/// Webhook event type'larindan domain event'lere esleyen router.
/// order.created → OrderReceivedEvent, order.cancelled → OrderCancelledEvent, vb.
/// MediatR IPublisher ile event'leri broadcast eder.
/// </summary>
public sealed class WebhookEventRouter
{
    private readonly IPublisher _publisher;
    private readonly ILogger<WebhookEventRouter> _logger;

    /// <summary>
    /// Platform-agnostic event type → handler mapping.
    /// Farkli platformlarin benzer event isimlerini normalize eder.
    /// </summary>
    private static readonly Dictionary<string, string> EventTypeAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        // Order events
        ["order.created"] = "order.created",
        ["order_created"] = "order.created",
        ["orders/create"] = "order.created",
        ["order.new"] = "order.created",

        // Order cancelled events
        ["order.cancelled"] = "order.cancelled",
        ["order_cancelled"] = "order.cancelled",
        ["orders/cancelled"] = "order.cancelled",
        ["order.cancel"] = "order.cancelled",

        // Order shipped events
        ["order.shipped"] = "order.shipped",
        ["order_shipped"] = "order.shipped",
        ["orders/fulfilled"] = "order.shipped",

        // Stock events
        ["stock.updated"] = "stock.updated",
        ["stock_updated"] = "stock.updated",
        ["inventory_levels/update"] = "stock.updated",

        // Product events
        ["product.updated"] = "product.updated",
        ["product_updated"] = "product.updated",
        ["products/update"] = "product.updated",

        // Product created events
        ["product.created"] = "product.created",
        ["product_created"] = "product.created",
        ["products/create"] = "product.created",

        // Return events
        ["return.created"] = "return.created",
        ["return_created"] = "return.created",
        ["refunds/create"] = "return.created",

        // Invoice events
        ["invoice.created"] = "invoice.created",
        ["invoice_created"] = "invoice.created",
    };

    public WebhookEventRouter(IPublisher publisher, ILogger<WebhookEventRouter> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// Webhook event type'ini normalize edip uygun domain event'i publish eder.
    /// </summary>
    public async Task<string?> RouteAsync(
        string platform,
        string eventType,
        string payload,
        CancellationToken ct)
    {
        var normalizedType = NormalizeEventType(eventType);

        _logger.LogInformation(
            "Webhook routing: platform={Platform}, raw={RawType}, normalized={NormalizedType}",
            platform, eventType, normalizedType ?? "UNKNOWN");

        if (normalizedType is null)
        {
            _logger.LogWarning(
                "Unknown webhook event type: platform={Platform}, eventType={EventType}",
                platform, eventType);
            return null;
        }

        var now = DateTime.UtcNow;

        switch (normalizedType)
        {
            case "order.created":
                var orderId = ExtractGuidField(payload, "orderId") ?? Guid.NewGuid();
                var orderTenantId = ExtractGuidField(payload, "tenantId") ?? Guid.NewGuid();
                var platformOrderId = ExtractStringField(payload, "platformOrderId")
                    ?? ExtractStringField(payload, "id")
                    ?? "unknown";
                var totalAmount = ExtractDecimalField(payload, "totalAmount")
                    ?? ExtractDecimalField(payload, "total_price")
                    ?? 0m;

                await PublishAsync(new OrderReceivedEvent(
                    orderId, orderTenantId, platform, platformOrderId, totalAmount, now), ct);
                break;

            case "order.cancelled":
                var cancelOrderId = ExtractGuidField(payload, "orderId") ?? Guid.NewGuid();
                var cancelTenantId = ExtractGuidField(payload, "tenantId") ?? Guid.NewGuid();
                var cancelPlatformOrderId = ExtractStringField(payload, "platformOrderId")
                    ?? ExtractStringField(payload, "id")
                    ?? "unknown";
                var reason = ExtractStringField(payload, "reason")
                    ?? ExtractStringField(payload, "cancel_reason");

                await PublishAsync(new OrderCancelledEvent(
                    cancelOrderId, cancelTenantId, platform, cancelPlatformOrderId, reason, now), ct);
                break;

            case "product.created":
                var createdProductId = ExtractGuidField(payload, "productId") ?? Guid.NewGuid();
                var createdTenantId = ExtractGuidField(payload, "tenantId") ?? Guid.NewGuid();
                var createdSku = ExtractStringField(payload, "sku") ?? "unknown";
                var createdName = ExtractStringField(payload, "name") ?? "Webhook Product";
                var createdPrice = ExtractDecimalField(payload, "salePrice")
                    ?? ExtractDecimalField(payload, "price")
                    ?? 0m;

                await PublishAsync(new ProductCreatedEvent(
                    createdProductId, createdTenantId, createdSku, createdName, createdPrice, now), ct);
                break;

            case "product.updated":
                var updatedProductId = ExtractGuidField(payload, "productId") ?? Guid.NewGuid();
                var updatedTenantId = ExtractGuidField(payload, "tenantId") ?? Guid.NewGuid();
                var updatedSku = ExtractStringField(payload, "sku") ?? "unknown";

                await PublishAsync(new ProductUpdatedEvent(
                    updatedProductId, updatedTenantId, updatedSku, now), ct);
                break;

            case "order.shipped":
                var shippedOrderId = ExtractGuidField(payload, "orderId") ?? Guid.NewGuid();
                var shippedTenantId = ExtractGuidField(payload, "tenantId") ?? Guid.NewGuid();
                var trackingNumber = ExtractStringField(payload, "trackingNumber")
                    ?? ExtractStringField(payload, "tracking_number")
                    ?? "unknown";
                var carrierStr = ExtractStringField(payload, "carrier")
                    ?? ExtractStringField(payload, "cargoProvider")
                    ?? "Unknown";
                var cargoProvider = Enum.TryParse<Domain.Enums.CargoProvider>(carrierStr, true, out var cp)
                    ? cp : Domain.Enums.CargoProvider.None;

                await PublishAsync(new OrderShippedEvent(
                    shippedOrderId, shippedTenantId, trackingNumber, cargoProvider, now), ct);
                break;

            case "stock.updated":
                var stockProductId = ExtractGuidField(payload, "productId") ?? Guid.NewGuid();
                var stockTenantId = ExtractGuidField(payload, "tenantId") ?? Guid.NewGuid();
                var stockSku = ExtractStringField(payload, "sku") ?? "unknown";
                var newQuantity = (int)(ExtractDecimalField(payload, "quantity")
                    ?? ExtractDecimalField(payload, "available")
                    ?? 0);

                await PublishAsync(new StockChangedEvent(
                    stockProductId, stockTenantId, stockSku, 0, newQuantity,
                    StockMovementType.PlatformSync, now), ct);
                break;

            case "return.created":
                var returnId = ExtractGuidField(payload, "returnId") ?? Guid.NewGuid();
                var returnTenantId = ExtractGuidField(payload, "tenantId") ?? Guid.NewGuid();
                var returnOrderId = ExtractGuidField(payload, "orderId") ?? Guid.NewGuid();
                var returnPlatform = ResolvePlatformType(platform);

                await PublishAsync(new ReturnCreatedEvent(
                    returnId, returnTenantId, returnOrderId, returnPlatform, ReturnReason.Other, now), ct);
                break;

            case "invoice.created":
                var invoiceId = ExtractGuidField(payload, "invoiceId") ?? Guid.NewGuid();
                var invoiceOrderId = ExtractGuidField(payload, "orderId") ?? Guid.NewGuid();
                var invoiceTenantId = ExtractGuidField(payload, "tenantId") ?? Guid.NewGuid();
                var grandTotal = ExtractDecimalField(payload, "grandTotal")
                    ?? ExtractDecimalField(payload, "total")
                    ?? 0m;

                await PublishAsync(new InvoiceCreatedEvent(
                    invoiceId, invoiceOrderId, invoiceTenantId,
                    InvoiceType.EFatura, grandTotal, now), ct);
                break;

            default:
                _logger.LogDebug(
                    "No domain event mapping for normalized type: {NormalizedType}",
                    normalizedType);
                return normalizedType;
        }

        _logger.LogInformation(
            "Webhook event routed successfully: platform={Platform}, event={EventType}",
            platform, normalizedType);

        return normalizedType;
    }

    private static string? NormalizeEventType(string eventType)
    {
        return EventTypeAliases.TryGetValue(eventType, out var normalized)
            ? normalized
            : null;
    }

    private static PlatformType ResolvePlatformType(string platform)
    {
        return platform.ToLowerInvariant() switch
        {
            "trendyol" => PlatformType.Trendyol,
            "shopify" => PlatformType.OpenCart, // Shopify → OpenCart enum slot (closest match)
            "woocommerce" => PlatformType.OpenCart,
            "hepsiburada" => PlatformType.Hepsiburada,
            "n11" => PlatformType.N11,
            "amazon" => PlatformType.Amazon,
            "ciceksepeti" => PlatformType.Ciceksepeti,
            "ebay" => PlatformType.eBay,
            _ => PlatformType.OpenCart
        };
    }

    private async Task PublishAsync<T>(T domainEvent, CancellationToken ct)
        where T : Domain.Common.IDomainEvent
    {
        var notification = new DomainEventNotification<T>(domainEvent);
        await _publisher.Publish(notification, ct);
    }

    private static Guid? ExtractGuidField(string json, string fieldName)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(fieldName, out var prop) &&
                prop.TryGetGuid(out var value))
                return value;
        }
        catch { /* Best-effort extraction */ }
        return null;
    }

    private static string? ExtractStringField(string json, string fieldName)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(fieldName, out var prop))
                return prop.GetString();
        }
        catch { /* Best-effort extraction */ }
        return null;
    }

    private static decimal? ExtractDecimalField(string json, string fieldName)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(fieldName, out var prop) &&
                prop.TryGetDecimal(out var value))
                return value;
        }
        catch { /* Best-effort extraction */ }
        return null;
    }
}
