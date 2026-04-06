using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

/// <summary>
/// Teklif faturaya dönüştürüldüğünde tetiklenir.
/// Handler: stok rezervasyonu, muhasebe kaydı, bildirim.
/// </summary>
public record QuotationConvertedEvent(
    Guid QuotationId,
    Guid TenantId,
    string QuotationNumber,
    Guid InvoiceId,
    Guid? CustomerId,
    decimal GrandTotal,
    DateTime OccurredAt
) : IDomainEvent;
