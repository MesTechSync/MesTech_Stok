using MesTech.Domain.Common;

namespace MesTech.Domain.Events.EInvoice;

public record EInvoiceCancelledEvent(
    Guid EInvoiceId,
    string EttnNo,
    string Reason,
    DateTime OccurredAt
) : IDomainEvent;
