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
        ArgumentNullException.ThrowIfNull(request);
        var unmatchedBatches = await _settlementRepo.GetUnmatchedAsync(request.TenantId, cancellationToken);
        var unreconciledTxs = await _bankTxRepo.GetUnreconciledAsync(request.TenantId, cancellationToken);

        var counters = new ReconciliationCounters();
        var matchedTxIds = new HashSet<Guid>();

        foreach (var batch in unmatchedBatches)
        {
            await ProcessBatchAsync(batch, unreconciledTxs, matchedTxIds, counters, request.TenantId, cancellationToken);
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return new RunReconciliationResult
        {
            AutoMatchedCount = counters.AutoMatchedCount,
            NeedsReviewCount = counters.NeedsReviewCount,
            UnmatchedCount = counters.UnmatchedCount,
            AutoMatchedTotal = counters.AutoMatchedTotal,
            NeedsReviewTotal = counters.NeedsReviewTotal
        };
    }

    private async Task ProcessBatchAsync(
        SettlementBatch batch,
        IReadOnlyList<BankTransaction> unreconciledTxs,
        HashSet<Guid> matchedTxIds,
        ReconciliationCounters counters,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var (bestScore, bestTx) = FindBestMatch(batch, unreconciledTxs, matchedTxIds);

        if (bestScore >= _scoringService.AutoMatchThreshold && bestTx != null)
        {
            await CreateMatchAsync(tenantId, bestScore, ReconciliationStatus.AutoMatched, batch, bestTx, matchedTxIds, cancellationToken);
            counters.AutoMatchedCount++;
            counters.AutoMatchedTotal += batch.TotalNet;
        }
        else if (bestScore >= _scoringService.ReviewThreshold && bestTx != null)
        {
            await CreateMatchAsync(tenantId, bestScore, ReconciliationStatus.NeedsReview, batch, bestTx, matchedTxIds, cancellationToken);
            counters.NeedsReviewCount++;
            counters.NeedsReviewTotal += batch.TotalNet;
        }
        else
        {
            counters.UnmatchedCount++;
        }
    }

    private (decimal BestScore, BankTransaction? BestTx) FindBestMatch(
        SettlementBatch batch,
        IReadOnlyList<BankTransaction> unreconciledTxs,
        HashSet<Guid> matchedTxIds)
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

        return (bestScore, bestTx);
    }

    private async Task CreateMatchAsync(
        Guid tenantId,
        decimal score,
        ReconciliationStatus status,
        SettlementBatch batch,
        BankTransaction bestTx,
        HashSet<Guid> matchedTxIds,
        CancellationToken cancellationToken)
    {
        var match = ReconciliationMatch.Create(
            tenantId,
            DateTime.UtcNow,
            score,
            status,
            batch.Id,
            bestTx.Id);

        await _matchRepo.AddAsync(match, cancellationToken);

        bestTx.MarkReconciled();
        await _bankTxRepo.UpdateAsync(bestTx, cancellationToken);

        if (status == ReconciliationStatus.AutoMatched)
        {
            batch.MarkReconciled();
            await _settlementRepo.UpdateAsync(batch, cancellationToken);
        }

        matchedTxIds.Add(bestTx.Id);
    }

    private sealed class ReconciliationCounters
    {
        public int AutoMatchedCount { get; set; }
        public int NeedsReviewCount { get; set; }
        public int UnmatchedCount { get; set; }
        public decimal AutoMatchedTotal { get; set; }
        public decimal NeedsReviewTotal { get; set; }
    }
}
