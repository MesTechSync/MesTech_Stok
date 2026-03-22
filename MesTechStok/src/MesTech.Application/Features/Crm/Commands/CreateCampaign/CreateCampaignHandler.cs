using MediatR;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Crm.Commands.CreateCampaign;

public class CreateCampaignHandler : IRequestHandler<CreateCampaignCommand, Guid>
{
    private readonly ICampaignRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateCampaignHandler(ICampaignRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateCampaignCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var campaign = Campaign.Create(
            request.TenantId,
            request.Name,
            request.StartDate,
            request.EndDate,
            request.DiscountPercent,
            request.PlatformType);

        if (request.ProductIds is { Length: > 0 })
        {
            foreach (var productId in request.ProductIds)
            {
                var campaignProduct = CampaignProduct.Create(campaign.Id, productId);
                campaign.AddProduct(campaignProduct);
            }
        }

        await _repository.AddAsync(campaign, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return campaign.Id;
    }
}
