using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

/// <summary>
/// Teklif reddedildiğinde tetiklenir.
/// Handler: CRM deal kaybı, analiz kaydı, bildirim.
/// </summary>
public record QuotationRejectedEvent(
    Guid QuotationId,
    Guid TenantId,
    string QuotationNumber,
    Guid? CustomerId,
    string CustomerName,
    decimal GrandTotal,
    DateTime OccurredAt
) : IDomainEvent;
