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

public record OrderShippedIntegrationEvent(
    Guid OrderId,
    string TrackingNumber,
    string CargoProvider,
    Guid TenantId,
    DateTime OccurredAt
);

public record ProductUpdatedIntegrationEvent(
    Guid ProductId,
    string SKU,
    string UpdatedField,
    Guid TenantId,
    DateTime OccurredAt
);

// ════════════════════════════════════════
// DALGA 8 — CRM Integration Events
// ════════════════════════════════════════

/// <summary>Lead CrmContact'a dönüştürüldü — MESA AI'ya bildir.</summary>
public record LeadConvertedIntegrationEvent(
    Guid LeadId,
    Guid CrmContactId,
    string FullName,
    string? Email,
    Guid TenantId,
    DateTime OccurredAt
);

/// <summary>Deal kazanıldı — MESA Bot WhatsApp tebrik gönderecek.</summary>
public record DealWonIntegrationEvent(
    Guid DealId,
    string DealTitle,
    decimal Amount,
    Guid? OrderId,
    Guid? CrmContactId,
    Guid TenantId,
    DateTime OccurredAt
);

/// <summary>Deal kaybedildi — MESA AI kayıp analizi yapacak.</summary>
public record DealLostIntegrationEvent(
    Guid DealId,
    string DealTitle,
    string Reason,
    decimal Amount,
    Guid TenantId,
    DateTime OccurredAt
);
