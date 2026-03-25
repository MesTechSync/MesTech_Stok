using MediatR;
using MesTech.Domain.Entities;

namespace MesTech.Application.Features.System.Queries.GetAuditLogs;

public sealed class GetAuditLogsHandler : IRequestHandler<GetAuditLogsQuery, IReadOnlyList<AccessLog>>
{
    public Task<IReadOnlyList<AccessLog>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<AccessLog>>(Array.Empty<AccessLog>());
    }
}
