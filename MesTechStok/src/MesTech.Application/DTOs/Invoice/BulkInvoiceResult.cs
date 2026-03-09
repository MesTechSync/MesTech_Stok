namespace MesTech.Application.DTOs.Invoice;

public record BulkInvoiceResult(
    int TotalRequested,
    int SuccessCount,
    int FailedCount,
    IReadOnlyList<BulkInvoiceItemResult> Results);

public record BulkInvoiceItemResult(
    Guid OrderId,
    bool Success,
    string? GibInvoiceId,
    string? ErrorMessage);
