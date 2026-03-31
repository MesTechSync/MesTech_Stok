using MediatR;

namespace MesTech.Application.Features.Reporting.Commands.ExportReport;

/// <summary>
/// Genel rapor disa aktarma komutu — ReportDashboardAvaloniaViewModel + ReportAvaloniaViewModel.
/// </summary>
public sealed record ExportReportCommand(
    Guid TenantId,
    string ReportType,
    string Format = "xlsx",
    Dictionary<string, string>? Parameters = null
) : IRequest<ExportReportResult>;
