using MediatR;
using MesTech.Domain.Entities.Erp;

namespace MesTech.Application.Features.Erp.Queries.GetErpSyncHistory;

public record GetErpSyncHistoryQuery(Guid TenantId, int Page = 1, int PageSize = 20) : IRequest<IReadOnlyList<ErpSyncLog>>;
