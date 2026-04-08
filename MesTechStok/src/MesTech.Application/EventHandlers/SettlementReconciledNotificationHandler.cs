using MesTech.Application.Interfaces.Accounting;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Settlement batch reconcile edildiginde calisir.
/// Tetikleyici: SettlementReconciledEvent (SettlementBatch.MarkReconciled icinden)
/// Gorev: Audit log + bildirim. GL kaydi ayri handler'da (SettlementPaymentGLHandler).
/// </summary>
public interface ISettlementReconciledNotificationHandler
{
    Task HandleAsync(Guid settlementBatchId, Guid tenantId, string platform, decimal totalNet, CancellationToken ct);
}

public sealed class SettlementReconciledNotificationHandler : ISettlementReconciledNotificationHandler
{
    private readonly ISettlementBatchRepository _settlementRepo;
    private readonly ILogger<SettlementReconciledNotificationHandler> _logger;

    public SettlementReconciledNotificationHandler(
        ISettlementBatchRepository settlementRepo,
        ILogger<SettlementReconciledNotificationHandler> logger)
    {
        _settlementRepo = settlementRepo;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid settlementBatchId, Guid tenantId, string platform, decimal totalNet, CancellationToken ct)
    {
        _logger.LogInformation(
            "Settlement RECONCILED — BatchId={BatchId}, Platform={Platform}, Net={Net:F2}, TenantId={TenantId}",
            settlementBatchId, platform, totalNet, tenantId);

        var batch = await _settlementRepo.GetByIdAsync(settlementBatchId, ct).ConfigureAwait(false);
        if (batch is null)
        {
            _logger.LogWarning("SettlementBatch {BatchId} bulunamadi — bildirim atlaniyor", settlementBatchId);
            return;
        }

        _logger.LogInformation(
            "Settlement reconciled ozet — Platform={Platform}, Donem={Start:d}–{End:d}, " +
            "Brut={Gross:F2}, Komisyon={Commission:F2}, Net={Net:F2}, Satirlar={LineCount}",
            batch.Platform, batch.PeriodStart, batch.PeriodEnd,
            batch.TotalGross, batch.TotalCommission, batch.TotalNet, batch.Lines.Count);
    }
}
