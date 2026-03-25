using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

/// <summary>
/// Fatura kabul edildiğinde yayınlanır.
/// Muhasebe kayıt onayı, ERP sync tetikleyici.
/// </summary>
public record InvoiceAcceptedEvent(
    Guid InvoiceId,
    Guid TenantId,
    string InvoiceNumber,
    decimal GrandTotal,
    DateTime OccurredAt
) : IDomainEvent;
