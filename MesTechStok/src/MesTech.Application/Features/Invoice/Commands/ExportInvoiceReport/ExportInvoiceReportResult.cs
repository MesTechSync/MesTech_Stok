namespace MesTech.Application.Features.Invoice.Commands.ExportInvoiceReport;

/// <summary>
/// Fatura raporu disa aktarma sonucu.
/// </summary>
public sealed class ExportInvoiceReportResult
{
    public byte[] FileData { get; set; } = [];
    public string FileName { get; set; } = string.Empty;
    public int ExportedCount { get; set; }
}
