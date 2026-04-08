using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Crm.Commands.DeleteCampaign;

public sealed class DeleteCampaignHandler : IRequestHandler<DeleteCampaignCommand, DeleteCampaignResult>
{
    private readonly ICampaignRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeleteCampaignHandler(ICampaignRepository repository, IUnitOfWork uow)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public async Task<DeleteCampaignResult> Handle(DeleteCampaignCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return new DeleteCampaignResult(false, $"Kampanya {request.Id} bulunamadı.");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new DeleteCampaignResult(true);
    }
}
