using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events.EInvoice;

public record EInvoiceCreatedEvent(
    Guid EInvoiceId,
    string EttnNo,
    EInvoiceType Type,
    DateTime OccurredAt
) : IDomainEvent;
