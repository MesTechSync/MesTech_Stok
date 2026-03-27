using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events;

/// <summary>
/// Fatura onaylandi — gonderime hazir.
/// Taslak → Onayli gecisinde uretilir.
/// </summary>
public record InvoiceApprovedEvent(
    Guid InvoiceId,
    Guid TenantId,
    string InvoiceNumber,
    decimal GrandTotal,
    decimal TaxAmount,
    decimal NetAmount,
    InvoiceType Type,
    DateTime OccurredAt) : IDomainEvent;
