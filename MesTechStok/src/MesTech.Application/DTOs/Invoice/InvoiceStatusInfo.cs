using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs.Invoice;

/// <summary>
/// Invoice Status Info data transfer object.
/// </summary>
public record InvoiceStatusInfo(
    string GibInvoiceId,
    InvoiceStatus Status,
    string? Description,
    DateTime? ResponseDate,
    string? RejectReason);
