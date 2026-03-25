using MediatR;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Crm.Commands.CreateDeal;

public sealed class CreateDealHandler : IRequestHandler<CreateDealCommand, Guid>
{
    private readonly ICrmDealRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateDealHandler(ICrmDealRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateDealCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var deal = Deal.Create(
            request.TenantId,
            request.Title,
            request.PipelineId,
            request.StageId,
            request.Amount,
            request.CrmContactId,
            request.ExpectedCloseDate,
            request.AssignedToUserId,
            request.StoreId);

        await _repository.AddAsync(deal, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return deal.Id;
    }
}
