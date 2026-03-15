using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Crm.Commands.WinDeal;

public class WinDealHandler : IRequestHandler<WinDealCommand, Unit>
{
    private readonly ICrmDealRepository _deals;
    private readonly IUnitOfWork _uow;

    public WinDealHandler(ICrmDealRepository deals, IUnitOfWork uow)
        => (_deals, _uow) = (deals, uow);

    public async Task<Unit> Handle(WinDealCommand req, CancellationToken ct)
    {
        var deal = await _deals.GetByIdAsync(req.DealId, ct)
            ?? throw new InvalidOperationException($"Deal {req.DealId} not found.");

        deal.MarkAsWon(req.OrderId);
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
