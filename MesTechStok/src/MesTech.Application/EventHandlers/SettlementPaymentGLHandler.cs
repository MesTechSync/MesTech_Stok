using MesTech.Domain.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Hakedis tahsilat GL handler (Zincir 4b).
/// Mutabakat eşleştirmesi tamamlanıp onaylandığında:
///   BORÇ 102 Bankalar (banka hesabına para geldi)
///   ALACAK 120 Alıcılar (platform alacağı kapandı)
/// Tetikleyici: ReconciliationCompletedEvent (FinalStatus = Approved/AutoMatched)
/// </summary>
public interface ISettlementPaymentGLHandler
{
    Task HandleAsync(
        Guid matchId, Guid tenantId, Guid? settlementBatchId,
        Guid? bankTransactionId, ReconciliationStatus finalStatus,
        CancellationToken ct);
}

public sealed class SettlementPaymentGLHandler : ISettlementPaymentGLHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJournalEntryRepository _journalRepo;
    private readonly ISettlementBatchRepository _settlementRepo;
    private readonly ILogger<SettlementPaymentGLHandler> _logger;

    public SettlementPaymentGLHandler(
        IUnitOfWork unitOfWork,
        IJournalEntryRepository journalRepo,
        ISettlementBatchRepository settlementRepo,
        ILogger<SettlementPaymentGLHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _journalRepo = journalRepo;
        _settlementRepo = settlementRepo;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid matchId, Guid tenantId, Guid? settlementBatchId,
        Guid? bankTransactionId, ReconciliationStatus finalStatus,
        CancellationToken ct)
    {
        // Sadece onaylanan eşleşmeler için GL kaydı oluştur
        if (finalStatus != ReconciliationStatus.AutoMatched && finalStatus != ReconciliationStatus.ManualMatch)
        {
            _logger.LogDebug("Reconciliation status {Status} — GL atlanıyor. MatchId={MatchId}",
                finalStatus, matchId);
            return;
        }

        if (!settlementBatchId.HasValue)
        {
            _logger.LogWarning("SettlementBatchId null — GL atlanıyor. MatchId={MatchId}", matchId);
            return;
        }

        var refNumber = $"SETTLEMENT-{matchId:N}".Substring(0, 40);

        // Idempotency guard
        if (await _journalRepo.ExistsByReferenceAsync(tenantId, refNumber, ct))
        {
            _logger.LogWarning("Duplicate settlement GL — ref {Ref} zaten var, atlanıyor", refNumber);
            return;
        }

        var batch = await _settlementRepo.GetByIdAsync(settlementBatchId.Value, ct).ConfigureAwait(false);
        if (batch is null)
        {
            _logger.LogError("SettlementBatch bulunamadı — GL atlanıyor. BatchId={BatchId}", settlementBatchId);
            return;
        }

        var amount = batch.TotalNet;
        if (amount <= 0)
        {
            _logger.LogDebug("Settlement tutarı 0 — GL atlanıyor. BatchId={BatchId}", settlementBatchId);
            return;
        }

        _logger.LogInformation(
            "Hakedis tahsilat GL oluşturuluyor — Platform={Platform}, Tutar={Amount}, MatchId={MatchId}",
            batch.Platform, amount, matchId);

        var entry = JournalEntry.Create(
            tenantId,
            DateTime.UtcNow,
            $"Hakedis tahsilat — {batch.Platform} #{batch.PeriodEnd:yyyy-MM-dd}",
            refNumber);

        // BORÇ: 102 Bankalar (banka hesabına para geldi)
        entry.AddLine(AccountingConstants.Account102Banks, amount, 0,
            $"102 Bankalar — {batch.Platform} hakedis");

        // ALACAK: 120 Alıcılar (platform alacağı kapandı)
        entry.AddLine(AccountingConstants.Account120Receivables, 0, amount,
            $"120 Alıcılar — {batch.Platform} tahsilat");

        entry.Validate();
        entry.Post();

        await _journalRepo.AddAsync(entry, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Hakedis tahsilat GL tamamlandı — EntryId={EntryId}, Platform={Platform}, Tutar={Amount}",
            entry.Id, batch.Platform, amount);
    }
}
