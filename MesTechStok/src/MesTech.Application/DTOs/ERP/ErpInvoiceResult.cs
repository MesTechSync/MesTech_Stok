namespace MesTech.Application.DTOs.ERP;

/// <summary>
/// ERP fatura islem sonucu.
/// </summary>
public record ErpInvoiceResult(
    bool Success,
    string? InvoiceNumber,
    string? ErpRef,
    DateTime? InvoiceDate,
    decimal? GrandTotal,
    string? PdfUrl,
    string? ErrorMessage
)
{
    public static ErpInvoiceResult Ok(string invoiceNumber, string erpRef, DateTime invoiceDate, decimal grandTotal, string? pdfUrl = null)
        => new(true, invoiceNumber, erpRef, invoiceDate, grandTotal, pdfUrl, null);

    public static ErpInvoiceResult Failed(string error)
        => new(false, null, null, null, null, null, error);
}
