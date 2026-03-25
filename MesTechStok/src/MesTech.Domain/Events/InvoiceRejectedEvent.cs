using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

/// <summary>
/// Fatura reddedildiğinde yayınlanır.
/// Bildirim + yeniden düzenleme tetikleyici.
/// </summary>
public record InvoiceRejectedEvent(
    Guid InvoiceId,
    Guid TenantId,
    string InvoiceNumber,
    DateTime OccurredAt
) : IDomainEvent;
