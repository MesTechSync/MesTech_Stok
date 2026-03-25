using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Crm.Commands.DeactivateCampaign;

public sealed class DeactivateCampaignHandler : IRequestHandler<DeactivateCampaignCommand, Unit>
{
    private readonly ICampaignRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeactivateCampaignHandler(ICampaignRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Unit> Handle(DeactivateCampaignCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var campaign = await _repository.GetByIdAsync(request.CampaignId, cancellationToken)
            ?? throw new InvalidOperationException($"Campaign '{request.CampaignId}' not found.");

        campaign.Deactivate();
        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
