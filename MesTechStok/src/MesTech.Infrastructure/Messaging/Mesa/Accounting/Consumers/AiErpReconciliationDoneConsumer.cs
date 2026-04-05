using MassTransit;
using MediatR;
using MesTech.Application.Commands.FinalizeErpReconciliation;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;

/// <summary>
/// MESA AI ERP uzlastirmasi tamamladi — uyusmazliklar raporlanir.
/// Her mismatch icin NeedsReview statulu ReconciliationMatch olusturur.
/// </summary>
public sealed class AiErpReconciliationDoneConsumer : IConsumer<AiErpReconciliationDoneIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly IReconciliationMatchRepository _matchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<AiErpReconciliationDoneConsumer> _logger;

    public AiErpReconciliationDoneConsumer(
        IMediator mediator,
        IReconciliationMatchRepository matchRepository,
        IUnitOfWork unitOfWork,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<AiErpReconciliationDoneConsumer> logger)
    {
        _mediator = mediator;
        _matchRepository = matchRepository;
        _unitOfWork = unitOfWork;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AiErpReconciliationDoneIntegrationEvent> context)
    {
        var msg = context.Message;
        var tenantId = msg.TenantId;
        if (tenantId == Guid.Empty)
        {
            tenantId = _tenantProvider.GetCurrentTenantId();
            _logger.LogWarning(
                "[MESA Consumer] Event without TenantId, using default {TenantId}", tenantId);
        }

        if (tenantId == Guid.Empty)
        {
            _logger.LogError(
                "[MESA Consumer] TenantId is Guid.Empty after fallback — aborting. MessageId={MessageId}",
                context.MessageId);
            _monitor.RecordError("ai.erp.reconciliation.done", "TenantId is Guid.Empty — aborted");
            throw new InvalidOperationException("TenantId is Guid.Empty — message rejected to prevent cross-tenant data leak");
        }

        _logger.LogInformation(
            "Processing {Event} — {Id}",
            nameof(AiErpReconciliationDoneIntegrationEvent), context.MessageId);

        try
        {
            await _mediator.Send(new FinalizeErpReconciliationCommand
            {
                ErpProvider = msg.ErpProvider,
                ReconciledCount = msg.ReconciledCount,
                MismatchCount = msg.MismatchCount,
                TenantId = tenantId
            }, context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {Event}", nameof(AiErpReconciliationDoneIntegrationEvent));
            throw; // Let MassTransit retry policy handle
        }

        _logger.LogInformation(
            "[MESA Consumer] AI ERP uzlastirma tamamlandi: ErpProvider={ErpProvider}, " +
            "ReconciledCount={ReconciledCount}, MismatchCount={MismatchCount}, TenantId={TenantId}",
            msg.ErpProvider, msg.ReconciledCount, msg.MismatchCount, tenantId);

        try
        {
            // Create a NeedsReview ReconciliationMatch for each mismatch reported by AI
            for (var i = 0; i < msg.MismatchCount; i++)
            {
                var match = ReconciliationMatch.Create(
                    tenantId: tenantId,
                    matchDate: DateTime.UtcNow,
                    confidence: 0.0m, // AI ERP mismatch — no confidence, needs human review
                    status: ReconciliationStatus.NeedsReview);

                await _matchRepository.AddAsync(match, context.CancellationToken).ConfigureAwait(false);

                _logger.LogDebug(
                    "[MESA Consumer] ERP mismatch ReconciliationMatch olusturuldu: " +
                    "MatchId={MatchId}, ErpProvider={ErpProvider}, Index={Index}",
                    match.Id, msg.ErpProvider, i + 1);
            }

            if (msg.MismatchCount > 0)
            {
                await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "[MESA Consumer] AI ERP uzlastirma islendi: " +
                "ErpProvider={ErpProvider}, OlusturulanMismatch={MismatchCount}, Reconciled={ReconciledCount}",
                msg.ErpProvider, msg.MismatchCount, msg.ReconciledCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MESA Consumer] AI ERP uzlastirma islenirken hata: ErpProvider={ErpProvider}",
                msg.ErpProvider);
            throw; // MassTransit retry policy
        }

        _monitor.RecordConsume("ai.erp.reconciliation.done");
    }
}
