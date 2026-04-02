using MassTransit;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Services;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs.Accounting;

/// <summary>
/// Otomatik mutabakat eslestirme worker — her 4 saatte bir calisir.
/// Eslestirilmemis SettlementBatch ve BankTransaction ciftlerini tarar,
/// IReconciliationScoringService ile skor hesaplar ve sonuca gore isler:
///   >= 0.95 → AutoMatched (otomatik eslestirme)
///   0.70-0.94 → NeedsReview (inceleme gerektiren, MESA event publish)
///   &lt; 0.70 → skip (eslestirilemez)
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class ReconciliationWorker : IAccountingJob
{
    public string JobId => "accounting-reconciliation";
    public string CronExpression => "0 */4 * * *"; // Her 4 saatte bir

    private readonly ISettlementBatchRepository _settlementRepo;
    private readonly IBankTransactionRepository _bankTxRepo;
    private readonly IReconciliationMatchRepository _matchRepo;
    private readonly IReconciliationScoringService _scoringService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ReconciliationWorker> _logger;

    public ReconciliationWorker(
        ISettlementBatchRepository settlementRepo,
        IBankTransactionRepository bankTxRepo,
        IReconciliationMatchRepository matchRepo,
        IReconciliationScoringService scoringService,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<ReconciliationWorker> logger)
    {
        _settlementRepo = settlementRepo;
        _bankTxRepo = bankTxRepo;
        _matchRepo = matchRepo;
        _scoringService = scoringService;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Mutabakat eslestirme basliyor...", JobId);

        var tenantId = _tenantProvider.GetCurrentTenantId();

        try
        {
            // 1. Eslestirilmemis SettlementBatch'leri al
            var unmatchedBatches = await _settlementRepo.GetUnmatchedAsync(tenantId, ct).ConfigureAwait(false);

            // 2. Eslesmemis BankTransaction'lari al
            var unreconciledTxs = await _bankTxRepo.GetUnreconciledAsync(tenantId, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[{JobId}] {BatchCount} eslestirilmemis batch, {TxCount} eslenmemis banka hareketi bulundu",
                JobId, unmatchedBatches.Count, unreconciledTxs.Count);

            if (unmatchedBatches.Count == 0 || unreconciledTxs.Count == 0)
            {
                _logger.LogInformation(
                    "[{JobId}] Eslestirme yapilacak veri yok, atlaniyor", JobId);
                return;
            }

            var autoMatched = 0;
            var needsReview = 0;
            var unmatched = 0;
            var matchedTxIds = new HashSet<Guid>();

            foreach (var batch in unmatchedBatches)
            {
                ct.ThrowIfCancellationRequested();

                var bestScore = 0m;
                BankTransaction? bestTx = null;

                // 3. Her cift icin skor hesapla
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
                    // 4. >= 0.95 → AutoMatched
                    var match = ReconciliationMatch.Create(
                        tenantId,
                        DateTime.UtcNow,
                        bestScore,
                        ReconciliationStatus.AutoMatched,
                        batch.Id,
                        bestTx.Id);

                    await _matchRepo.AddAsync(match, ct).ConfigureAwait(false);

                    bestTx.MarkReconciled();
                    await _bankTxRepo.UpdateAsync(bestTx, ct).ConfigureAwait(false);

                    batch.MarkReconciled();
                    await _settlementRepo.UpdateAsync(batch, ct).ConfigureAwait(false);

                    matchedTxIds.Add(bestTx.Id);
                    autoMatched++;

                    // Idempotency guard (G086): per-match save — crash durumunda
                    // eşleştirilen batch+tx tekrar GetUnmatchedAsync'te gelmez
                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                }
                else if (bestScore >= _scoringService.ReviewThreshold && bestTx != null)
                {
                    // 5. 0.70-0.94 → NeedsReview + publish event
                    var match = ReconciliationMatch.Create(
                        tenantId,
                        DateTime.UtcNow,
                        bestScore,
                        ReconciliationStatus.NeedsReview,
                        batch.Id,
                        bestTx.Id);

                    await _matchRepo.AddAsync(match, ct).ConfigureAwait(false);
                    matchedTxIds.Add(bestTx.Id);
                    needsReview++;

                    // Per-match save (G086)
                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

                    // MESA'ya bildir
                    await _publishEndpoint.Publish(new FinanceReconciliationPendingEvent(
                        MatchId: match.Id,
                        SettlementBatchId: batch.Id,
                        BankTransactionId: bestTx.Id,
                        Confidence: bestScore,
                        SettlementAmount: batch.TotalNet,
                        BankTxAmount: bestTx.Amount,
                        TenantId: tenantId,
                        OccurredAt: DateTime.UtcNow), ct).ConfigureAwait(false);
                }
                else
                {
                    // 6. < 0.70 → skip
                    unmatched++;
                }
            }

            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            // 7. Ozet log
            _logger.LogInformation(
                "[{JobId}] Reconciliation complete: {Auto} auto-matched, {Review} needs review, {Unmatched} unmatched",
                JobId, autoMatched, needsReview, unmatched);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobId}] Mutabakat eslestirme HATA", JobId);
            throw;
        }
    }
}
