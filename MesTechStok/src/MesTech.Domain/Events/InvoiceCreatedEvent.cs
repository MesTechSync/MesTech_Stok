using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events;

public record InvoiceCreatedEvent(
    Guid InvoiceId,
    Guid OrderId,
    InvoiceType Type,
    decimal GrandTotal,
    DateTime OccurredAt
) : IDomainEvent;
