using MediatR;
using MesTech.Application.Behaviors;
using MesTech.Application.DTOs.Crm;

namespace MesTech.Application.Features.Crm.Queries.GetActiveCampaigns;

public record GetActiveCampaignsQuery(Guid TenantId)
    : IRequest<GetActiveCampaignsResult>, ICacheableQuery
{
    public string CacheKey => $"ActiveCampaigns_{TenantId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(3);
}

public sealed class GetActiveCampaignsResult
{
    public IReadOnlyList<CampaignDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
}
