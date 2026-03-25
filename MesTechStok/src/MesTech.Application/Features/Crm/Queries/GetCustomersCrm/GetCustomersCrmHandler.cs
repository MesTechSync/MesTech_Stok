using MediatR;
using MesTech.Application.DTOs.Crm;
using MesTech.Application.Interfaces;

namespace MesTech.Application.Features.Crm.Queries.GetCustomersCrm;

public sealed class GetCustomersCrmHandler : IRequestHandler<GetCustomersCrmQuery, GetCustomersCrmResult>
{
    private readonly ICrmDashboardQueryService _queryService;

    public GetCustomersCrmHandler(ICrmDashboardQueryService queryService)
        => _queryService = queryService;

    public async Task<GetCustomersCrmResult> Handle(GetCustomersCrmQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (items, totalCount) = await _queryService.GetCustomersPagedAsync(
            request.TenantId, request.IsVip, request.IsActive, request.SearchTerm,
            request.Page, request.PageSize, cancellationToken);

        return new GetCustomersCrmResult
        {
            Items = items,
            TotalCount = totalCount
        };
    }
}
