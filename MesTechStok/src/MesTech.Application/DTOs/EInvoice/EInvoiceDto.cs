using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs.EInvoice;

public record EInvoiceDto(
    Guid Id,
    string GibUuid,
    string EttnNo,
    EInvoiceScenario Scenario,
    EInvoiceType Type,
    EInvoiceStatus Status,
    DateTime IssueDate,
    DateTime DueDate,
    string SellerVkn,
    string SellerTitle,
    string? BuyerVkn,
    string BuyerTitle,
    decimal PayableAmount,
    string CurrencyCode,
    string ProviderId,
    string? ProviderRef,
    string? PdfUrl,
    int CreditUsed,
    DateTime CreatedAt);

public record EInvoiceLineDto(
    int LineNumber,
    string Description,
    decimal Quantity,
    string UnitCode,
    decimal UnitPrice,
    decimal TaxAmount,
    int TaxPercent,
    decimal LineExtensionAmount);
