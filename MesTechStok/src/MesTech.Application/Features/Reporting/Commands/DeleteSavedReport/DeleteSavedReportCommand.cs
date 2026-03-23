using MediatR;

namespace MesTech.Application.Features.Reporting.Commands.DeleteSavedReport;

/// <summary>
/// Kaydedilmis rapor silme komutu.
/// </summary>
public record DeleteSavedReportCommand(
    Guid TenantId,
    Guid ReportId
) : IRequest<bool>;
