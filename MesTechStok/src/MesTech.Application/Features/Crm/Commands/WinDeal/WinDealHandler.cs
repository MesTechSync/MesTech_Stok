using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Crm.Commands.WinDeal;

public sealed class WinDealHandler : IRequestHandler<WinDealCommand, Unit>
{
    private readonly ICrmDealRepository _deals;
    private readonly IUnitOfWork _uow;

    public WinDealHandler(ICrmDealRepository deals, IUnitOfWork uow)
        => (_deals, _uow) = (deals, uow);

    public async Task<Unit> Handle(WinDealCommand request, CancellationToken cancellationToken)
    {
        var deal = await _deals.GetByIdAsync(request.DealId, cancellationToken)
            ?? throw new InvalidOperationException($"Deal {request.DealId} not found.");

        deal.MarkAsWon(request.OrderId);
        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
