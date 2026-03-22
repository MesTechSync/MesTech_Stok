using MediatR;
using MesTech.Application.DTOs.Crm;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;

namespace MesTech.Application.Features.Crm.Queries.ApplyCampaignDiscount;

public class ApplyCampaignDiscountHandler : IRequestHandler<ApplyCampaignDiscountQuery, CampaignDiscountResultDto>
{
    private readonly ICampaignRepository _repository;
    private readonly PricingService _pricingService;

    public ApplyCampaignDiscountHandler(ICampaignRepository repository, PricingService pricingService)
        => (_repository, _pricingService) = (repository, pricingService);

    public async Task<CampaignDiscountResultDto> Handle(ApplyCampaignDiscountQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var campaigns = await _repository.GetActiveByProductIdAsync(request.ProductId, cancellationToken);

        var activeCampaigns = campaigns
            .Where(c => c.IsCurrentlyActive())
            .ToList();

        if (activeCampaigns.Count == 0)
        {
            return new CampaignDiscountResultDto
            {
                OriginalPrice = request.Price,
                DiscountedPrice = request.Price,
                AppliedCampaignName = string.Empty,
                DiscountPercent = 0
            };
        }

        // Pick the campaign with the highest discount percentage (best for customer)
        var bestCampaign = activeCampaigns
            .OrderByDescending(c => c.DiscountPercent)
            .First();

        var discountedPrice = _pricingService.ApplyDiscount(request.Price, bestCampaign.DiscountPercent);

        return new CampaignDiscountResultDto
        {
            OriginalPrice = request.Price,
            DiscountedPrice = discountedPrice,
            AppliedCampaignName = bestCampaign.Name,
            DiscountPercent = bestCampaign.DiscountPercent
        };
    }
}
