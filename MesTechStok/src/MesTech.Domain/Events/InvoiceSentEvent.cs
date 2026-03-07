using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record InvoiceSentEvent(
    Guid InvoiceId,
    string? GibInvoiceId,
    string? PdfUrl,
    DateTime OccurredAt
) : IDomainEvent;
