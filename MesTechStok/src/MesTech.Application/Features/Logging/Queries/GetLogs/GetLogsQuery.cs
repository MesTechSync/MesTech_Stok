using MediatR;
using MesTech.Domain.Entities;

namespace MesTech.Application.Features.Logging.Queries.GetLogs;

public record GetLogsQuery(
    Guid TenantId,
    int Page = 1,
    int PageSize = 50,
    string? Category = null,
    string? UserId = null,
    string? ProductName = null,
    string? Barcode = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null
) : IRequest<IReadOnlyList<LogEntry>>;
