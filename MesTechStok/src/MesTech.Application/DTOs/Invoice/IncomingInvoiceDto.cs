using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs.Invoice;

public record IncomingInvoiceDto(
    string GibInvoiceId,
    string InvoiceNumber,
    string SenderName,
    string SenderTaxNumber,
    decimal GrandTotal,
    DateTime InvoiceDate,
    string? PdfUrl,
    InvoiceStatus Status);
