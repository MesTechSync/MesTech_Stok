using MediatR;
using MesTech.Domain.Entities.Erp;

namespace MesTech.Application.Features.Erp.Queries.GetErpSyncLogs;

public record GetErpSyncLogsQuery(
    Guid TenantId,
    int Page = 1,
    int PageSize = 20
) : IRequest<IReadOnlyList<ErpSyncLog>>;
