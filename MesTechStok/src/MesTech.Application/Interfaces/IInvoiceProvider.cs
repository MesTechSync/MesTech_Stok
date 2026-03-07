namespace MesTech.Application.Interfaces;

public interface IInvoiceProvider
{
    string ProviderName { get; }
    Task<InvoiceResult> CreateEFaturaAsync(InvoiceDto invoice, CancellationToken ct = default);
    Task<InvoiceResult> CreateEArsivAsync(InvoiceDto invoice, CancellationToken ct = default);
    Task<InvoiceResult> CreateEIrsaliyeAsync(InvoiceDto invoice, CancellationToken ct = default);
    Task<InvoiceStatusResult> CheckStatusAsync(string gibInvoiceId, CancellationToken ct = default);
    Task<byte[]> GetPdfAsync(string gibInvoiceId, CancellationToken ct = default);
    Task<bool> IsEInvoiceTaxpayerAsync(string taxNumber, CancellationToken ct = default);
    Task<InvoiceResult> CancelInvoiceAsync(string gibInvoiceId, CancellationToken ct = default);
}

public record InvoiceDto(
    string InvoiceNumber,
    string CustomerName,
    string? CustomerTaxNumber,
    string? CustomerTaxOffice,
    string CustomerAddress,
    decimal SubTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    IReadOnlyList<InvoiceLineDto> Lines
);

public record InvoiceLineDto(
    string ProductName,
    string? SKU,
    int Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal TaxAmount,
    decimal LineTotal
);

public record InvoiceResult(
    bool Success,
    string? GibInvoiceId,
    string? PdfUrl,
    string? ErrorMessage
);

public record InvoiceStatusResult(
    string GibInvoiceId,
    string Status,
    DateTime? AcceptedAt,
    string? ErrorMessage
);
