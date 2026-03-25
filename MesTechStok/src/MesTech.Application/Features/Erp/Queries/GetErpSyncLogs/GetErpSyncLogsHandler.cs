using MediatR;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Entities.Erp;

namespace MesTech.Application.Features.Erp.Queries.GetErpSyncLogs;

public sealed class GetErpSyncLogsHandler : IRequestHandler<GetErpSyncLogsQuery, IReadOnlyList<ErpSyncLog>>
{
    private readonly IErpSyncLogRepository _repo;

    public GetErpSyncLogsHandler(IErpSyncLogRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<ErpSyncLog>> Handle(GetErpSyncLogsQuery request, CancellationToken cancellationToken)
    {
        return await _repo.GetByTenantPagedAsync(request.TenantId, request.Page, request.PageSize, cancellationToken);
    }
}
