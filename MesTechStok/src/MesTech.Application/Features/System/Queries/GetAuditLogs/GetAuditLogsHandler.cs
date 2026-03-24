using MediatR;
using MesTech.Domain.Entities;

namespace MesTech.Application.Features.System.Queries.GetAuditLogs;

public class GetAuditLogsHandler : IRequestHandler<GetAuditLogsQuery, IReadOnlyList<AccessLog>>
{
    public Task<IReadOnlyList<AccessLog>> Handle(GetAuditLogsQuery request, CancellationToken ct)
    {
        return Task.FromResult<IReadOnlyList<AccessLog>>(Array.Empty<AccessLog>());
    }
}
