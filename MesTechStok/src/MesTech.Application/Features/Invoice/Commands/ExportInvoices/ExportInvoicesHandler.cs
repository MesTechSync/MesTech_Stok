using MediatR;

namespace MesTech.Application.Features.Invoice.Commands.ExportInvoices;

/// <summary>
/// Fatura disa aktarma handler'i.
/// Stub: Gercek Excel/CSV uretimi Infrastructure katmaninda (IExcelExportService) yapilacak.
/// </summary>
public sealed class ExportInvoicesHandler : IRequestHandler<ExportInvoicesCommand, ExportInvoicesResult>
{
    public Task<ExportInvoicesResult> Handle(
        ExportInvoicesCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var extension = request.Format.ToUpperInvariant() switch
        {
            "CSV" => "csv",
            "PDF" => "pdf",
            _ => "xlsx"
        };

        var fromPart = request.DateFrom?.ToString("yyyyMMdd") ?? "all";
        var toPart = request.DateTo?.ToString("yyyyMMdd") ?? "all";

        var result = new ExportInvoicesResult
        {
            FileData = [], // Infrastructure concern — IExcelExportService / ICsvExportService
            FileName = $"faturalar_{fromPart}_{toPart}.{extension}",
            ExportedCount = 0
        };

        return Task.FromResult(result);
    }
}
