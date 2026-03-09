using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs.Invoice;

public record InvoiceStatusInfo(
    string GibInvoiceId,
    InvoiceStatus Status,
    string? Description,
    DateTime? ResponseDate,
    string? RejectReason);
