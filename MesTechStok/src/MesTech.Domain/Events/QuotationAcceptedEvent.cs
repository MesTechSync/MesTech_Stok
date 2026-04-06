using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

/// <summary>
/// Teklif kabul edildiğinde tetiklenir.
/// Handler: otomatik fatura oluşturma, CRM deal güncelleme, bildirim.
/// </summary>
public record QuotationAcceptedEvent(
    Guid QuotationId,
    Guid TenantId,
    string QuotationNumber,
    Guid? CustomerId,
    string CustomerName,
    decimal GrandTotal,
    string Currency,
    DateTime OccurredAt
) : IDomainEvent;
