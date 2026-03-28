using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;

namespace MesTech.Application.Features.System.Queries.GetAuditLogs;

public sealed class GetAuditLogsHandler : IRequestHandler<GetAuditLogsQuery, IReadOnlyList<AccessLog>>
{
    private readonly IAccessLogRepository _repository;

    public GetAuditLogsHandler(IAccessLogRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<AccessLog>> Handle(
        GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetPagedAsync(
            request.TenantId,
            request.From,
            request.To,
            request.UserId is not null ? Guid.Parse(request.UserId) : null,
            request.Action,
            request.Page,
            Math.Clamp(request.PageSize, 1, 100),
            cancellationToken);
    }
}
