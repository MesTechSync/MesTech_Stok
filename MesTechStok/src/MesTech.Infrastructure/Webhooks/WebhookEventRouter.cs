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

        // Parse payload ONCE — avoid repeated JsonDocument.Parse per field extraction
        JsonElement root;
        try
        {
            using var doc = JsonDocument.Parse(payload);
            root = doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Webhook payload is not valid JSON: platform={Platform}", platform);
            return null;
        }

        var now = DateTime.UtcNow;

        switch (normalizedType)
        {
            case "order.created":
                var orderId = GetGuid(root, "orderId") ?? Guid.NewGuid();
                var orderTenantId = GetGuid(root, "tenantId") ?? Guid.NewGuid();
                var platformOrderId = GetString(root, "platformOrderId")
                    ?? GetString(root, "id")
                    ?? "unknown";
                var totalAmount = GetDecimal(root, "totalAmount")
                    ?? GetDecimal(root, "total_price")
                    ?? 0m;

                await PublishAsync(new OrderReceivedEvent(
                    orderId, orderTenantId, platform, platformOrderId, totalAmount, now), ct);
                break;

            case "order.cancelled":
                var cancelOrderId = GetGuid(root, "orderId") ?? Guid.NewGuid();
                var cancelTenantId = GetGuid(root, "tenantId") ?? Guid.NewGuid();
                var cancelPlatformOrderId = GetString(root, "platformOrderId")
                    ?? GetString(root, "id")
                    ?? "unknown";
                var reason = GetString(root, "reason")
                    ?? GetString(root, "cancel_reason");

                await PublishAsync(new OrderCancelledEvent(
                    cancelOrderId, cancelTenantId, platform, cancelPlatformOrderId, reason, now), ct);
                break;

            case "product.created":
                var createdProductId = GetGuid(root, "productId") ?? Guid.NewGuid();
                var createdTenantId = GetGuid(root, "tenantId") ?? Guid.NewGuid();
                var createdSku = GetString(root, "sku") ?? "unknown";
                var createdName = GetString(root, "name") ?? "Webhook Product";
                var createdPrice = GetDecimal(root, "salePrice")
                    ?? GetDecimal(root, "price")
                    ?? 0m;

                await PublishAsync(new ProductCreatedEvent(
                    createdProductId, createdTenantId, createdSku, createdName, createdPrice, now), ct);
                break;

            case "product.updated":
                var updatedProductId = GetGuid(root, "productId") ?? Guid.NewGuid();
                var updatedTenantId = GetGuid(root, "tenantId") ?? Guid.NewGuid();
                var updatedSku = GetString(root, "sku") ?? "unknown";

                await PublishAsync(new ProductUpdatedEvent(
                    updatedProductId, updatedTenantId, updatedSku, now), ct);
                break;

            case "order.shipped":
                var shippedOrderId = GetGuid(root, "orderId") ?? Guid.NewGuid();
                var shippedTenantId = GetGuid(root, "tenantId") ?? Guid.NewGuid();
                var trackingNumber = GetString(root, "trackingNumber")
                    ?? GetString(root, "tracking_number")
                    ?? "unknown";
                var carrierStr = GetString(root, "carrier")
                    ?? GetString(root, "cargoProvider")
                    ?? "Unknown";
                var cargoProvider = Enum.TryParse<Domain.Enums.CargoProvider>(carrierStr, true, out var cp)
                    ? cp : Domain.Enums.CargoProvider.None;

                await PublishAsync(new OrderShippedEvent(
                    shippedOrderId, shippedTenantId, trackingNumber, cargoProvider, now), ct);
                break;

            case "stock.updated":
                var stockProductId = GetGuid(root, "productId") ?? Guid.NewGuid();
                var stockTenantId = GetGuid(root, "tenantId") ?? Guid.NewGuid();
                var stockSku = GetString(root, "sku") ?? "unknown";
                var newQuantity = (int)(GetDecimal(root, "quantity")
                    ?? GetDecimal(root, "available")
                    ?? 0);

                await PublishAsync(new StockChangedEvent(
                    stockProductId, stockTenantId, stockSku, 0, newQuantity,
                    StockMovementType.PlatformSync, now), ct);
                break;

            case "return.created":
                var returnId = GetGuid(root, "returnId") ?? Guid.NewGuid();
                var returnTenantId = GetGuid(root, "tenantId") ?? Guid.NewGuid();
                var returnOrderId = GetGuid(root, "orderId") ?? Guid.NewGuid();
                var returnPlatform = ResolvePlatformType(platform);

                await PublishAsync(new ReturnCreatedEvent(
                    returnId, returnTenantId, returnOrderId, returnPlatform, ReturnReason.Other, now), ct);
                break;

            case "invoice.created":
                var invoiceId = GetGuid(root, "invoiceId") ?? Guid.NewGuid();
                var invoiceOrderId = GetGuid(root, "orderId") ?? Guid.NewGuid();
                var invoiceTenantId = GetGuid(root, "tenantId") ?? Guid.NewGuid();
                var grandTotal = GetDecimal(root, "grandTotal")
                    ?? GetDecimal(root, "total")
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
            "hepsiburada" => PlatformType.Hepsiburada,
            "n11" => PlatformType.N11,
            "ciceksepeti" => PlatformType.Ciceksepeti,
            "amazon" => PlatformType.Amazon,
            "amazoneu" or "amazon_eu" or "amazon-eu" => PlatformType.AmazonEu,
            "ebay" => PlatformType.eBay,
            "etsy" => PlatformType.Etsy,
            "ozon" => PlatformType.Ozon,
            "shopify" => PlatformType.Shopify,
            "woocommerce" => PlatformType.WooCommerce,
            "zalando" => PlatformType.Zalando,
            "pazarama" => PlatformType.Pazarama,
            "pttavm" or "ptavm" => PlatformType.PttAVM,
            "opencart" => PlatformType.OpenCart,
            "bitrix24" or "bitrix" => PlatformType.Bitrix24,
            _ => Enum.TryParse<PlatformType>(platform, true, out var parsed)
                ? parsed
                : PlatformType.OpenCart
        };
    }

    private async Task PublishAsync<T>(T domainEvent, CancellationToken ct)
        where T : Domain.Common.IDomainEvent
    {
        var notification = new DomainEventNotification<T>(domainEvent);
        await _publisher.Publish(notification, ct);
    }

    private static Guid? GetGuid(JsonElement root, string fieldName)
        => root.TryGetProperty(fieldName, out var prop) && prop.TryGetGuid(out var v) ? v : null;

    private static string? GetString(JsonElement root, string fieldName)
        => root.TryGetProperty(fieldName, out var prop) ? prop.GetString() : null;

    private static decimal? GetDecimal(JsonElement root, string fieldName)
        => root.TryGetProperty(fieldName, out var prop) && prop.TryGetDecimal(out var v) ? v : null;
}
