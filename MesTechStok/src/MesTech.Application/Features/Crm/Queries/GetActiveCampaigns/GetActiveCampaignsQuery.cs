using MediatR;
using MesTech.Application.DTOs.Crm;

namespace MesTech.Application.Features.Crm.Queries.GetActiveCampaigns;

public record GetActiveCampaignsQuery(Guid TenantId) : IRequest<GetActiveCampaignsResult>;

public sealed class GetActiveCampaignsResult
{
    public IReadOnlyList<CampaignDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
}
