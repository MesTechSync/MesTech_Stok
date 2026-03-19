namespace MesTech.Application.Features.Invoice.DTOs;

public record BulkInvoiceResultDto(
    int TotalRequested,
    int SuccessCount,
    int FailCount,
    List<BulkInvoiceItemResultDto> Results);

public record BulkInvoiceItemResultDto(
    Guid OrderId,
    string OrderNumber,
    bool Success,
    string? InvoiceNumber,
    string? ErrorMessage);
