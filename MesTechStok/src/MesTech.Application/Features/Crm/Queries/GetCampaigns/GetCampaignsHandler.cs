using MediatR;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;
namespace MesTech.Application.Features.Crm.Queries.GetCampaigns;
public class GetCampaignsHandler : IRequestHandler<GetCampaignsQuery, IReadOnlyList<Campaign>>
{
    private readonly ICampaignRepository _repo;
    public GetCampaignsHandler(ICampaignRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<Campaign>> Handle(GetCampaignsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await _repo.GetByTenantAsync(request.TenantId, request.ActiveOnly, cancellationToken);
    }
}
