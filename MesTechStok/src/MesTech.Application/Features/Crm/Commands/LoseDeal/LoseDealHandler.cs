using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Crm.Commands.LoseDeal;

public class LoseDealHandler : IRequestHandler<LoseDealCommand, Unit>
{
    private readonly ICrmDealRepository _deals;
    private readonly IUnitOfWork _uow;

    public LoseDealHandler(ICrmDealRepository deals, IUnitOfWork uow)
        => (_deals, _uow) = (deals, uow);

    public async Task<Unit> Handle(LoseDealCommand req, CancellationToken ct)
    {
        var deal = await _deals.GetByIdAsync(req.DealId, ct)
            ?? throw new InvalidOperationException($"Deal {req.DealId} not found.");

        deal.MarkAsLost(req.Reason);
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
