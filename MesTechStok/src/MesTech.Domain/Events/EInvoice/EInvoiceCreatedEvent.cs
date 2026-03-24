using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events.EInvoice;

public record EInvoiceCreatedEvent(
    Guid EInvoiceId,
    Guid TenantId,
    string EttnNo,
    EInvoiceType Type,
    DateTime OccurredAt
) : IDomainEvent;
