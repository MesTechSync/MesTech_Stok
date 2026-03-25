using MediatR;
using MesTech.Application.DTOs.Crm;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Crm.Queries.GetActiveCampaigns;

public sealed class GetActiveCampaignsHandler : IRequestHandler<GetActiveCampaignsQuery, GetActiveCampaignsResult>
{
    private readonly ICampaignRepository _repository;

    public GetActiveCampaignsHandler(ICampaignRepository repository)
        => _repository = repository;

    public async Task<GetActiveCampaignsResult> Handle(GetActiveCampaignsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var campaigns = await _repository.GetActiveByTenantAsync(request.TenantId, cancellationToken);

        var activeCampaigns = campaigns
            .Where(c => c.IsCurrentlyActive())
            .Select(c => new CampaignDto
            {
                Id = c.Id,
                Name = c.Name,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                DiscountPercent = c.DiscountPercent,
                PlatformType = c.PlatformType,
                IsActive = c.IsActive,
                ProductCount = c.Products.Count
            })
            .ToList()
            .AsReadOnly();

        return new GetActiveCampaignsResult
        {
            Items = activeCampaigns,
            TotalCount = activeCampaigns.Count
        };
    }
}
