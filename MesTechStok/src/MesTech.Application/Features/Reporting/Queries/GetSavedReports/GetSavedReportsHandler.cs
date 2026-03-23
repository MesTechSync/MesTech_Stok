using MediatR;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Reporting.Queries.GetSavedReports;

/// <summary>
/// Kaydedilmis rapor listeleme handler'i.
/// Tenant bazli filtreleme ile raporlari dondurur.
/// </summary>
public class GetSavedReportsHandler
    : IRequestHandler<GetSavedReportsQuery, IReadOnlyList<SavedReportListDto>>
{
    private readonly ISavedReportRepository _savedReportRepository;
    private readonly ILogger<GetSavedReportsHandler> _logger;

    public GetSavedReportsHandler(
        ISavedReportRepository savedReportRepository,
        ILogger<GetSavedReportsHandler> logger)
    {
        _savedReportRepository = savedReportRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SavedReportListDto>> Handle(
        GetSavedReportsQuery request, CancellationToken cancellationToken)
    {
        var reports = await _savedReportRepository.GetByTenantAsync(
            request.TenantId, cancellationToken);

        _logger.LogInformation(
            "Kaydedilmis raporlar listelendi: TenantId={TenantId}, Count={Count}",
            request.TenantId, reports.Count);

        return reports.Select(r => new SavedReportListDto(
            r.Id,
            r.Name,
            r.ReportType,
            r.CreatedAt,
            r.LastExecutedAt
        )).ToList();
    }
}
