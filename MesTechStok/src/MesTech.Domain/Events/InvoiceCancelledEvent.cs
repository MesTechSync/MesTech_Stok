using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record InvoiceCancelledEvent(
    Guid InvoiceId,
    Guid TenantId,
    Guid OrderId,
    string InvoiceNumber,
    string? Reason,
    DateTime OccurredAt
) : IDomainEvent;
