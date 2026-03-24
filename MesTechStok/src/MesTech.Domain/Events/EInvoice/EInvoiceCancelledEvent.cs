using MesTech.Domain.Common;

namespace MesTech.Domain.Events.EInvoice;

public record EInvoiceCancelledEvent(
    Guid EInvoiceId,
    Guid TenantId,
    string EttnNo,
    string Reason,
    DateTime OccurredAt
) : IDomainEvent;
