using MediatR;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Entities.Erp;

namespace MesTech.Application.Features.Erp.Queries.GetErpSyncHistory;

public sealed class GetErpSyncHistoryHandler : IRequestHandler<GetErpSyncHistoryQuery, IReadOnlyList<ErpSyncLog>>
{
    private readonly IErpSyncLogRepository _repo;
    public GetErpSyncHistoryHandler(IErpSyncLogRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<ErpSyncLog>> Handle(GetErpSyncHistoryQuery request, CancellationToken cancellationToken)
    {
        return await _repo.GetByTenantPagedAsync(request.TenantId, request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);
    }
}
