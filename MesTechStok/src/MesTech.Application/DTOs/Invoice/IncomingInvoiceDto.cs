namespace MesTech.Application.DTOs.Invoice;

public record IncomingInvoiceDto(
    string GibInvoiceId,
    string SenderName,
    string SenderTaxNumber,
    decimal Amount,
    DateTime InvoiceDate,
    string Status);
