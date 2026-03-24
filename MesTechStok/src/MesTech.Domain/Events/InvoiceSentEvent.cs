using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record InvoiceSentEvent(
    Guid InvoiceId,
    Guid TenantId,
    string? GibInvoiceId,
    string? PdfUrl,
    DateTime OccurredAt
) : IDomainEvent;
