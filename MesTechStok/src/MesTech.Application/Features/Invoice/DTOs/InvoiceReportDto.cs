namespace MesTech.Application.Features.Invoice.DTOs;

public record InvoiceReportDto(
    int TotalCount,
    decimal TotalAmount,
    int EFaturaCount,
    decimal EFaturaAmount,
    int EArsivCount,
    decimal EArsivAmount,
    int EIhracatCount,
    decimal EIhracatAmount,
    List<InvoiceReportByPlatformDto> ByPlatform);

public record InvoiceReportByPlatformDto(string PlatformName, int Count, decimal Amount);
