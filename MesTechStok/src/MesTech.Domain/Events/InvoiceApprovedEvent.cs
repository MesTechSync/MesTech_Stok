using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events;

/// <summary>
/// Fatura onaylandi — gonderime hazir.
/// Taslak → Onayli gecisinde uretilir.
/// </summary>
public record InvoiceApprovedEvent(
    Guid InvoiceId,
    string InvoiceNumber,
    decimal GrandTotal,
    InvoiceType Type,
    DateTime OccurredAt) : IDomainEvent;
