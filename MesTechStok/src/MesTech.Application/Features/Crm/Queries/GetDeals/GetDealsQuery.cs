using MediatR;
using MesTech.Application.DTOs.Crm;
using MesTech.Domain.Enums;
namespace MesTech.Application.Features.Crm.Queries.GetDeals;

public record GetDealsQuery(
    Guid TenantId,
    Guid? PipelineId = null,
    DealStatus? Status = null,
    Guid? AssignedToUserId = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<GetDealsResult>;

public class GetDealsResult
{
    public IReadOnlyList<DealDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
}
