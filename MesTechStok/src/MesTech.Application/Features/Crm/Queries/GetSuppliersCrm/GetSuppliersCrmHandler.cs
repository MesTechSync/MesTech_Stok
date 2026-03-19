using MediatR;
using MesTech.Application.DTOs.Crm;
using MesTech.Application.Interfaces;

namespace MesTech.Application.Features.Crm.Queries.GetSuppliersCrm;

public class GetSuppliersCrmHandler : IRequestHandler<GetSuppliersCrmQuery, GetSuppliersCrmResult>
{
    private readonly ICrmDashboardQueryService _queryService;

    public GetSuppliersCrmHandler(ICrmDashboardQueryService queryService)
        => _queryService = queryService;

    public async Task<GetSuppliersCrmResult> Handle(GetSuppliersCrmQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _queryService.GetSuppliersPagedAsync(
            request.TenantId, request.IsActive, request.IsPreferred, request.SearchTerm,
            request.Page, request.PageSize, cancellationToken);

        return new GetSuppliersCrmResult
        {
            Items = items,
            TotalCount = totalCount
        };
    }
}
