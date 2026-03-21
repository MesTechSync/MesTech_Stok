using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs.Invoice;

/// <summary>
/// Incoming Invoice data transfer object.
/// </summary>
public record IncomingInvoiceDto(
    string GibInvoiceId,
    string InvoiceNumber,
    string SenderName,
    string SenderTaxNumber,
    decimal GrandTotal,
    DateTime InvoiceDate,
    string? PdfUrl,
    InvoiceStatus Status);
