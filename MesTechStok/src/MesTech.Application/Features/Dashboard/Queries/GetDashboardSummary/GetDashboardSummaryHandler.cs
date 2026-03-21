using MesTech.Application.DTOs.Dashboard;
using MesTech.Application.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Dashboard.Queries.GetDashboardSummary;

/// <summary>
/// Unified dashboard summary handler.
/// Delegesi IDashboardSummaryRepository — sorgular Infrastructure katmanında.
/// Mevcut 6 Dashboard query'ye DOKUNMAZ.
/// </summary>
public class GetDashboardSummaryQueryHandler
    : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IDashboardSummaryRepository _repository;

    public GetDashboardSummaryQueryHandler(IDashboardSummaryRepository repository)
        => _repository = repository;

    public Task<DashboardSummaryDto> Handle(
        GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _repository.GetSummaryAsync(request.TenantId, cancellationToken);
    }
}
