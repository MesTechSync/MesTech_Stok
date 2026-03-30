using MediatR;

namespace MesTech.Application.Features.Crm.Queries.GetCrmActivities;

public record GetCrmActivitiesQuery(Guid TenantId, Guid? ContactId = null, int Page = 1, int PageSize = 50)
    : IRequest<CrmActivitiesResult>;

public sealed class CrmActivitiesResult
{
    public IReadOnlyList<CrmActivityDto> Activities { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
