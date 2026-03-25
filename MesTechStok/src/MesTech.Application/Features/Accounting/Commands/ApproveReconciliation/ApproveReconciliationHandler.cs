using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.ApproveReconciliation;

/// <summary>
/// Mutabakat eslestirme onaylama isleyicisi.
/// Status=ManualMatch, ReviewedBy ve ReviewedAt guncellenir.
/// Iliskilendirilen BankTransaction da reconciled olarak isaretlenir.
/// </summary>
public sealed class ApproveReconciliationHandler : IRequestHandler<ApproveReconciliationCommand, Unit>
{
    private readonly IReconciliationMatchRepository _matchRepo;
    private readonly IBankTransactionRepository _bankTxRepo;
    private readonly ISettlementBatchRepository _settlementRepo;
    private readonly IUnitOfWork _uow;

    public ApproveReconciliationHandler(
        IReconciliationMatchRepository matchRepo,
        IBankTransactionRepository bankTxRepo,
        ISettlementBatchRepository settlementRepo,
        IUnitOfWork uow)
    {
        _matchRepo = matchRepo;
        _bankTxRepo = bankTxRepo;
        _settlementRepo = settlementRepo;
        _uow = uow;
    }

    public async Task<Unit> Handle(ApproveReconciliationCommand request, CancellationToken cancellationToken)
    {
        var match = await _matchRepo.GetByIdAsync(request.MatchId, cancellationToken)
            ?? throw new InvalidOperationException($"ReconciliationMatch {request.MatchId} not found.");

        match.Approve(request.ReviewedBy.ToString());

        await _matchRepo.UpdateAsync(match, cancellationToken);

        // Mark bank transaction as reconciled
        if (match.BankTransactionId.HasValue)
        {
            var bankTx = await _bankTxRepo.GetByIdAsync(match.BankTransactionId.Value, cancellationToken);
            if (bankTx != null)
            {
                bankTx.MarkReconciled();
                await _bankTxRepo.UpdateAsync(bankTx, cancellationToken);
            }
        }

        // Mark settlement batch as reconciled
        if (match.SettlementBatchId.HasValue)
        {
            var batch = await _settlementRepo.GetByIdAsync(match.SettlementBatchId.Value, cancellationToken);
            if (batch != null)
            {
                batch.MarkReconciled();
                await _settlementRepo.UpdateAsync(batch, cancellationToken);
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
