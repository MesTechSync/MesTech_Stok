using MediatR;

namespace MesTech.Application.Features.Invoice.Commands.ExportInvoiceReport;

/// <summary>
/// Fatura raporu disa aktarma handler'i.
/// Stub: Gercek Excel/PDF uretimi Infrastructure katmaninda (IExcelExportService) yapilacak.
/// </summary>
public sealed class ExportInvoiceReportHandler : IRequestHandler<ExportInvoiceReportCommand, ExportInvoiceReportResult>
{
    public Task<ExportInvoiceReportResult> Handle(
        ExportInvoiceReportCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var extension = request.Format.ToUpperInvariant() switch
        {
            "CSV" => "csv",
            "PDF" => "pdf",
            _ => "xlsx"
        };

        var periodPart = request.Period ?? "all";
        var fromPart = request.DateFrom?.ToString("yyyyMMdd") ?? "all";
        var toPart = request.DateTo?.ToString("yyyyMMdd") ?? "all";

        var result = new ExportInvoiceReportResult
        {
            FileData = ReadOnlyMemory<byte>.Empty, // Infrastructure concern — IExcelExportService / IPdfExportService
            FileName = $"fatura_rapor_{periodPart}_{fromPart}_{toPart}.{extension}",
            ExportedCount = 0
        };

        return Task.FromResult(result);
    }
}
