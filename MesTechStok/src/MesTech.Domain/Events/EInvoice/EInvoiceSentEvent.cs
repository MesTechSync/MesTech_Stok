using MesTech.Domain.Common;

namespace MesTech.Domain.Events.EInvoice;

public record EInvoiceSentEvent(
    Guid EInvoiceId,
    string EttnNo,
    string? ProviderRef,
    DateTime OccurredAt
) : IDomainEvent;
