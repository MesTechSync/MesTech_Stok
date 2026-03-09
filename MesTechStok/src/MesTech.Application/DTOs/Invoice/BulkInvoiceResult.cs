namespace MesTech.Application.DTOs.Invoice;

public record BulkInvoiceResult(
    IReadOnlyList<MesTech.Application.DTOs.InvoiceResult> Results,
    int SuccessCount,
    int FailCount);
