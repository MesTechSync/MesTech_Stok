namespace MesTech.Application.Features.Invoice.DTOs;

public record InvoiceListDto(
    Guid Id,
    string InvoiceNumber,
    string? ExternalInvoiceId,
    string TypeName,
    string TypeBadgeColor,
    string StatusName,
    string StatusBadgeColor,
    string RecipientName,
    string? RecipientVKN,
    decimal TotalAmount,
    int TaxRate,
    string? PlatformName,
    string? ProviderName,
    DateTime InvoiceDate,
    DateTime? SentAt,
    bool HasPdf);
