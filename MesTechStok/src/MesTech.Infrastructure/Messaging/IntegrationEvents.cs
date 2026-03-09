namespace MesTech.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ uzerinden yayinlanan entegrasyon olaylari.
/// Domain event'lerden farkli: cross-service iletisim icin.
/// Dalga 5 IP-2: TenantId eklendi.
/// </summary>
public record StockChangedIntegrationEvent(
    Guid ProductId,
    string SKU,
    int NewQuantity,
    string Source,
    Guid TenantId,
    DateTime OccurredAt
);

public record PriceChangedIntegrationEvent(
    Guid ProductId,
    string SKU,
    decimal NewPrice,
    string Source,
    Guid TenantId,
    DateTime OccurredAt
);

public record OrderReceivedIntegrationEvent(
    Guid OrderId,
    string PlatformCode,
    string PlatformOrderId,
    decimal TotalAmount,
    Guid TenantId,
    DateTime OccurredAt
);

public record InvoiceCreatedIntegrationEvent(
    Guid InvoiceId,
    Guid OrderId,
    string InvoiceNumber,
    decimal GrandTotal,
    Guid TenantId,
    DateTime OccurredAt
);
