using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.RunReconciliation;

/// <summary>
/// Otomatik mutabakat eslestirme isleyicisi.
/// 1. Eslestirilmemis SettlementBatch'leri al
/// 2. Eslesmemis BankTransaction'lari al
/// 3. Her batch x tx cifti icin skor hesapla
/// 4. >= 0.95 → AutoMatched, 0.70-0.94 → NeedsReview, &lt; 0.70 → skip (Unmatched)
/// </summary>
public class RunReconciliationHandler : IRequestHandler<RunReconciliationCommand, RunReconciliationResult>
{
    private readonly ISettlementBatchRepository _settlementRepo;
    private readonly IBankTransactionRepository _bankTxRepo;
    private readonly IReconciliationMatchRepository _matchRepo;
    private readonly IReconciliationScoringService _scoringService;
    private readonly IUnitOfWork _uow;

    public RunReconciliationHandler(
        ISettlementBatchRepository settlementRepo,
        IBankTransactionRepository bankTxRepo,
        IReconciliationMatchRepository matchRepo,
        IReconciliationScoringService scoringService,
        IUnitOfWork uow)
    {
        _settlementRepo = settlementRepo;
        _bankTxRepo = bankTxRepo;
        _matchRepo = matchRepo;
        _scoringService = scoringService;
        _uow = uow;
    }

    public async Task<RunReconciliationResult> Handle(
        RunReconciliationCommand request,
        CancellationToken cancellationToken)
    {
        var unmatchedBatches = await _settlementRepo.GetUnmatchedAsync(request.TenantId, cancellationToken);
        var unreconciledTxs = await _bankTxRepo.GetUnreconciledAsync(request.TenantId, cancellationToken);

        var autoMatchedCount = 0;
        var needsReviewCount = 0;
        var unmatchedCount = 0;
        var autoMatchedTotal = 0m;
        var needsReviewTotal = 0m;

        // Track which bank transactions have been matched to avoid double-matching
        var matchedTxIds = new HashSet<Guid>();

        foreach (var batch in unmatchedBatches)
        {
            var bestScore = 0m;
            BankTransaction? bestTx = null;

            foreach (var tx in unreconciledTxs)
            {
                if (matchedTxIds.Contains(tx.Id))
                    continue;

                var score = _scoringService.CalculateConfidence(
                    tx.Amount,
                    batch.TotalNet,
                    tx.TransactionDate,
                    batch.PeriodEnd,
                    tx.Description,
                    batch.Platform);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTx = tx;
                }
            }

            if (bestScore >= _scoringService.AutoMatchThreshold && bestTx != null)
            {
                // AutoMatched
                var match = ReconciliationMatch.Create(
                    request.TenantId,
                    DateTime.UtcNow,
                    bestScore,
                    ReconciliationStatus.AutoMatched,
                    batch.Id,
                    bestTx.Id);

                await _matchRepo.AddAsync(match, cancellationToken);

                // Mark bank transaction as reconciled
                bestTx.MarkReconciled();
                await _bankTxRepo.UpdateAsync(bestTx, cancellationToken);

                // Mark settlement batch as reconciled
                batch.MarkReconciled();
                await _settlementRepo.UpdateAsync(batch, cancellationToken);

                matchedTxIds.Add(bestTx.Id);
                autoMatchedCount++;
                autoMatchedTotal += batch.TotalNet;
            }
            else if (bestScore >= _scoringService.ReviewThreshold && bestTx != null)
            {
                // NeedsReview
                var match = ReconciliationMatch.Create(
                    request.TenantId,
                    DateTime.UtcNow,
                    bestScore,
                    ReconciliationStatus.NeedsReview,
                    batch.Id,
                    bestTx.Id);

                await _matchRepo.AddAsync(match, cancellationToken);

                matchedTxIds.Add(bestTx.Id);
                needsReviewCount++;
                needsReviewTotal += batch.TotalNet;
            }
            else
            {
                // Unmatched — no match record created
                unmatchedCount++;
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return new RunReconciliationResult
        {
            AutoMatchedCount = autoMatchedCount,
            NeedsReviewCount = needsReviewCount,
            UnmatchedCount = unmatchedCount,
            AutoMatchedTotal = autoMatchedTotal,
            NeedsReviewTotal = needsReviewTotal
        };
    }
}
