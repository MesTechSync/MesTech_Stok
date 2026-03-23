using MediatR;

namespace MesTech.Application.Features.Logging.Queries.GetLogCount;

public record GetLogCountQuery(Guid TenantId, string? Category = null) : IRequest<long>;
