using MediatR;
using MesTech.Domain.Entities;

namespace MesTech.Application.Features.System.Queries.GetAuditLogs;

public record GetAuditLogsQuery(Guid TenantId, DateTime? From = null, DateTime? To = null, string? UserId = null, string? Action = null, int Page = 1, int PageSize = 50) : IRequest<IReadOnlyList<AccessLog>>;
