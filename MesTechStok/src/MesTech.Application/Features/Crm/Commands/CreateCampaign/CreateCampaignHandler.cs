using MediatR;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;
namespace MesTech.Application.Features.Crm.Commands.CreateCampaign;
public class CreateCampaignHandler : IRequestHandler<CreateCampaignCommand, Guid>
{
    private readonly ICampaignRepository _repo;
    public CreateCampaignHandler(ICampaignRepository repo) => _repo = repo;
    public async Task<Guid> Handle(CreateCampaignCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var campaign = Campaign.Create(request.TenantId, request.Name, request.StartDate, request.EndDate, request.DiscountPercent, request.PlatformType);
        await _repo.AddAsync(campaign, cancellationToken);
        return campaign.Id;
    }
}
