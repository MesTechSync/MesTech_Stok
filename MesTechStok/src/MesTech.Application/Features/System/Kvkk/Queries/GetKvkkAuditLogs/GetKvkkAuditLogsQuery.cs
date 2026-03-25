using MediatR;
using MesTech.Domain.Entities;

namespace MesTech.Application.Features.System.Kvkk.Queries.GetKvkkAuditLogs;

public record GetKvkkAuditLogsQuery(
    Guid TenantId,
    int Page = 1,
    int PageSize = 50
) : IRequest<KvkkAuditLogsResult>;

public sealed class KvkkAuditLogsResult
{
    public IReadOnlyList<KvkkAuditLogDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
}

public sealed class KvkkAuditLogDto
{
    public Guid Id { get; init; }
    public Guid RequestedByUserId { get; init; }
    public string OperationType { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public int AffectedRecordCount { get; init; }
    public bool IsSuccess { get; init; }
    public DateTime CompletedAt { get; init; }
}
