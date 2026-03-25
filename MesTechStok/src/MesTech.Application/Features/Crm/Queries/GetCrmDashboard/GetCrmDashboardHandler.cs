using MediatR;
using MesTech.Application.DTOs.Crm;
using MesTech.Application.Interfaces;

namespace MesTech.Application.Features.Crm.Queries.GetCrmDashboard;

public sealed class GetCrmDashboardHandler : IRequestHandler<GetCrmDashboardQuery, CrmDashboardDto>
{
    private readonly ICrmDashboardQueryService _queryService;

    public GetCrmDashboardHandler(ICrmDashboardQueryService queryService)
        => _queryService = queryService;

    public async Task<CrmDashboardDto> Handle(GetCrmDashboardQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await _queryService.GetDashboardAsync(request.TenantId, cancellationToken);
    }
}
