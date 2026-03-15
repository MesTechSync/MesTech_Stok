using MassTransit;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;

/// <summary>
/// MESA AI mutabakat eslestirme onerisi consume eder.
/// AI onerileri her zaman NeedsReview ile olusturulur — insan denetimi zorunlu.
/// </summary>
public class AiReconciliationSuggestedConsumer : IConsumer<AiReconciliationSuggestedEvent>
{
    private readonly IReconciliationMatchRepository _matchRepository;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AiReconciliationSuggestedConsumer> _logger;

    public AiReconciliationSuggestedConsumer(
        IReconciliationMatchRepository matchRepository,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork,
        ILogger<AiReconciliationSuggestedConsumer> logger)
    {
        _matchRepository = matchRepository;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AiReconciliationSuggestedEvent> context)
    {
        var msg = context.Message;
        var tenantId = msg.TenantId;
        if (tenantId == Guid.Empty)
        {
            tenantId = _tenantProvider.GetCurrentTenantId();
            _logger.LogWarning(
                "[MESA Consumer] Event without TenantId, using default {TenantId}", tenantId);
        }

        _logger.LogInformation(
            "[MESA Consumer] AI mutabakat onerisi alindi: " +
            "SettlementBatchId={SettlementBatchId}, BankTxId={BankTransactionId}, guven={Confidence:P0}",
            msg.SettlementBatchId, msg.BankTransactionId, msg.Confidence);

        // AI onerileri her zaman NeedsReview — insan denetimi zorunlu
        var match = ReconciliationMatch.Create(
            tenantId: tenantId,
            matchDate: DateTime.UtcNow,
            confidence: msg.Confidence,
            status: ReconciliationStatus.NeedsReview,
            settlementBatchId: msg.SettlementBatchId,
            bankTransactionId: msg.BankTransactionId);

        await _matchRepository.AddAsync(match);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "[MESA Consumer] AI oneri eslestirmesi kaydedildi (NeedsReview): " +
            "MatchId={MatchId}, guven={Confidence:P0}, aciklama={Rationale}",
            match.Id, msg.Confidence, msg.Rationale ?? "-");

        _monitor.RecordConsume("ai.reconciliation.suggested");
    }
}
