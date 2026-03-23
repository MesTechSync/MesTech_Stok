using MediatR;

namespace MesTech.Application.Features.Reporting.Queries.GetSavedReports;

/// <summary>
/// Tenant'a ait kaydedilmis raporlari listeler.
/// </summary>
public record GetSavedReportsQuery(
    Guid TenantId
) : IRequest<IReadOnlyList<SavedReportListDto>>;

/// <summary>
/// Kaydedilmis rapor liste DTO'su.
/// </summary>
public record SavedReportListDto(
    Guid Id,
    string Name,
    string ReportType,
    DateTime CreatedAt,
    DateTime? LastExecutedAt);
