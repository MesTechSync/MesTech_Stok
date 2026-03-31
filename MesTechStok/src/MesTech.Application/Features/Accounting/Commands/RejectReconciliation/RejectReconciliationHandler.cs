using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.RejectReconciliation;

/// <summary>
/// Mutabakat eslestirme reddetme isleyicisi.
/// Status=Rejected, ReviewedBy ve ReviewedAt guncellenir.
/// </summary>
public sealed class RejectReconciliationHandler : IRequestHandler<RejectReconciliationCommand, Unit>
{
    private readonly IReconciliationMatchRepository _matchRepo;
    private readonly IUnitOfWork _uow;

    public RejectReconciliationHandler(
        IReconciliationMatchRepository matchRepo,
        IUnitOfWork uow)
    {
        _matchRepo = matchRepo;
        _uow = uow;
    }

    public async Task<Unit> Handle(RejectReconciliationCommand request, CancellationToken cancellationToken)
    {
        var match = await _matchRepo.GetByIdAsync(request.MatchId, cancellationToken)
            ?? throw new InvalidOperationException($"ReconciliationMatch {request.MatchId} not found.");

        match.Reject(request.ReviewedBy.ToString());

        await _matchRepo.UpdateAsync(match, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
