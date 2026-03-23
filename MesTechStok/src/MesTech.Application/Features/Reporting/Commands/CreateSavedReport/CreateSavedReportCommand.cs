using MediatR;

namespace MesTech.Application.Features.Reporting.Commands.CreateSavedReport;

/// <summary>
/// Yeni kaydedilmis rapor olusturma komutu.
/// </summary>
public record CreateSavedReportCommand(
    Guid TenantId,
    string Name,
    string ReportType,
    string FilterJson,
    Guid CreatedByUserId
) : IRequest<Guid>;
