namespace MesTech.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ uzerinden yayinlanan entegrasyon olaylari.
/// Domain event'lerden farkli: cross-service iletisim icin.
/// </summary>
public record StockChangedIntegrationEvent(
    Guid ProductId,
    string SKU,
    int NewQuantity,
    string Source,
    DateTime OccurredAt
);

public record PriceChangedIntegrationEvent(
    Guid ProductId,
    string SKU,
    decimal NewPrice,
    string Source,
    DateTime OccurredAt
);

public record OrderReceivedIntegrationEvent(
    Guid OrderId,
    string PlatformCode,
    string PlatformOrderId,
    decimal TotalAmount,
    DateTime OccurredAt
);

public record InvoiceCreatedIntegrationEvent(
    Guid InvoiceId,
    Guid OrderId,
    string InvoiceNumber,
    decimal GrandTotal,
    DateTime OccurredAt
);
