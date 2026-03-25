using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Crm.Commands.LoseDeal;

public sealed class LoseDealHandler : IRequestHandler<LoseDealCommand, Unit>
{
    private readonly ICrmDealRepository _deals;
    private readonly IUnitOfWork _uow;

    public LoseDealHandler(ICrmDealRepository deals, IUnitOfWork uow)
        => (_deals, _uow) = (deals, uow);

    public async Task<Unit> Handle(LoseDealCommand request, CancellationToken cancellationToken)
    {
        var deal = await _deals.GetByIdAsync(request.DealId, cancellationToken)
            ?? throw new InvalidOperationException($"Deal {request.DealId} not found.");

        deal.MarkAsLost(request.Reason);
        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
