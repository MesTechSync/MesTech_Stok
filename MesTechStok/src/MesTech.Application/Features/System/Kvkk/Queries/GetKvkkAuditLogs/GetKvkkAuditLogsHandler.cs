using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.System.Kvkk.Queries.GetKvkkAuditLogs;

public sealed class GetKvkkAuditLogsHandler
    : IRequestHandler<GetKvkkAuditLogsQuery, KvkkAuditLogsResult>
{
    private readonly IKvkkAuditLogRepository _repository;

    public GetKvkkAuditLogsHandler(IKvkkAuditLogRepository repository)
        => _repository = repository;

    public async Task<KvkkAuditLogsResult> Handle(
        GetKvkkAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repository
            .GetByTenantPagedAsync(request.TenantId, request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        return new KvkkAuditLogsResult
        {
            Items = items.Select(log => new KvkkAuditLogDto
            {
                Id = log.Id,
                RequestedByUserId = log.RequestedByUserId,
                OperationType = log.OperationType.ToString(),
                Reason = log.Reason,
                AffectedRecordCount = log.AffectedRecordCount,
                IsSuccess = log.IsSuccess,
                CompletedAt = log.CompletedAt
            }).ToList(),
            TotalCount = total
        };
    }
}
