using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs.Accounting;

/// <summary>
/// Settlement→Order eslestirme batch job — her 6 saatte calisir.
/// Imported durumundaki SettlementBatch'lerin satirlarini tarar,
/// OrderNumber ile siparis eslestirir ve MarkAsPaid + SetCommission uygular.
/// Eslestirme sonrasi batch status Reconciled olarak guncellenir.
/// ReconciliationWorker (banka mutabakati) ile karistirilmamali —
/// bu job settlement→order odeme zincirini (Z4a) kapatan ayrı bir pipeline.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class SettlementOrderReconciliationJob : IAccountingJob
{
    public string JobId => "settlement-order-reconciliation";
    public string CronExpression => "0 */6 * * *"; // Her 6 saatte bir

    private readonly ISettlementBatchRepository _settlementRepo;
    private readonly ISettlementImportedOrderPaymentHandler _paymentHandler;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SettlementOrderReconciliationJob> _logger;

    public SettlementOrderReconciliationJob(
        ISettlementBatchRepository settlementRepo,
        ISettlementImportedOrderPaymentHandler paymentHandler,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        ILogger<SettlementOrderReconciliationJob> logger)
    {
        _settlementRepo = settlementRepo;
        _paymentHandler = paymentHandler;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Settlement→Order eslestirme basliyor...", JobId);

        var tenantId = _tenantProvider.GetCurrentTenantId();

        try
        {
            var unmatchedBatches = await _settlementRepo.GetUnmatchedAsync(tenantId, ct).ConfigureAwait(false);

            if (unmatchedBatches.Count == 0)
            {
                _logger.LogDebug("[{JobId}] Eslestirilecek batch yok", JobId);
                return;
            }

            _logger.LogInformation(
                "[{JobId}] {Count} eslestirilmemis settlement batch bulundu",
                JobId, unmatchedBatches.Count);

            int processed = 0, reconciled = 0, disputed = 0;

            foreach (var batch in unmatchedBatches)
            {
                ct.ThrowIfCancellationRequested();

                var result = await _paymentHandler.HandleAsync(batch.Id, tenantId, ct).ConfigureAwait(false);
                processed++;

                // Batch status guncelleme: tum satirlar eslestiyse → Reconciled, yoksa → Disputed
                if (result.TotalLines > 0)
                {
                    var matchRatio = (result.Matched + result.AlreadyPaid) / (decimal)result.TotalLines;

                    if (matchRatio >= 0.8m) // %80+ eslesti → Reconciled
                    {
                        batch.MarkReconciled();
                        reconciled++;
                    }
                    else if (result.NotFound > result.TotalLines / 2) // %50+ bulunamadi → Disputed
                    {
                        batch.MarkDisputed();
                        disputed++;
                    }
                    // else: kısmi eslestirme — Imported'da kal, sonraki turda tekrar dene

                    await _settlementRepo.UpdateAsync(batch, ct).ConfigureAwait(false);
                }
            }

            if (processed > 0)
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[{JobId}] Settlement→Order eslestirme tamamlandi: {Processed} batch islendi, {Reconciled} reconciled, {Disputed} disputed",
                JobId, processed, reconciled, disputed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[{JobId}] Settlement→Order eslestirme HATA", JobId);
            throw;
        }
    }
}
