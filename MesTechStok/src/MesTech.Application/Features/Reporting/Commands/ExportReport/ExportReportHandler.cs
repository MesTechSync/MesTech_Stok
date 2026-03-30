using MediatR;

namespace MesTech.Application.Features.Reporting.Commands.ExportReport;

/// <summary>
/// Genel rapor disa aktarma handler'i.
/// Stub: Gercek Excel/CSV/PDF uretimi Infrastructure katmaninda (IReportExportService) yapilacak.
/// </summary>
public sealed class ExportReportHandler : IRequestHandler<ExportReportCommand, ExportReportResult>
{
    public Task<ExportReportResult> Handle(
        ExportReportCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var extension = request.Format.ToUpperInvariant() switch
        {
            "CSV" => "csv",
            "PDF" => "pdf",
            _ => "xlsx"
        };

        var reportSlug = request.ReportType
            .Replace(" ", "_", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .ToUpperInvariant();

        var result = new ExportReportResult
        {
            FileData = [], // Infrastructure concern — IReportExportService
            FileName = $"rapor_{reportSlug}_{DateTime.UtcNow:yyyyMMddHHmmss}.{extension}",
            ExportedCount = 0
        };

        return Task.FromResult(result);
    }
}
