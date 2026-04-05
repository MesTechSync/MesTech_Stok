using MassTransit;
using MediatR;
using MesTech.Application.Commands.CreateEInvoiceFromDraft;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using DomainChannel = MesTech.Domain.Enums.NotificationChannel;

namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;

/// <summary>
/// MESA AI e-fatura taslagi olusturdu — muhasebe onayina gonder.
/// Taslak bilgisini NotificationLog olarak kaydeder, muhasebe ekibini bilgilendirir.
/// </summary>
public sealed class AiEInvoiceDraftGeneratedConsumer : IConsumer<AiEInvoiceDraftGeneratedIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly INotificationLogRepository _notificationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<AiEInvoiceDraftGeneratedConsumer> _logger;

    public AiEInvoiceDraftGeneratedConsumer(
        IMediator mediator,
        INotificationLogRepository notificationLogRepository,
        IUnitOfWork unitOfWork,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<AiEInvoiceDraftGeneratedConsumer> logger)
    {
        _mediator = mediator;
        _notificationLogRepository = notificationLogRepository;
        _unitOfWork = unitOfWork;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AiEInvoiceDraftGeneratedIntegrationEvent> context)
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
            _monitor.RecordError("ai.einvoice.draft.generated", "TenantId is Guid.Empty — aborted");
            throw new InvalidOperationException("TenantId is Guid.Empty — message rejected to prevent cross-tenant data leak");
        }

        _logger.LogInformation(
            "Processing {Event} — {Id}",
            nameof(AiEInvoiceDraftGeneratedIntegrationEvent), context.MessageId);

        try
        {
            await _mediator.Send(new CreateEInvoiceFromDraftCommand
            {
                OrderId = msg.OrderId,
                SuggestedEttnNo = msg.SuggestedEttnNo,
                SuggestedTotal = msg.SuggestedTotal,
                TenantId = tenantId
            }, context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to process {Event}", nameof(AiEInvoiceDraftGeneratedIntegrationEvent));
            throw; // Let MassTransit retry policy handle
        }

        _logger.LogInformation(
            "[MESA Consumer] AI e-fatura taslagi alindi: OrderId={OrderId}, " +
            "SuggestedEttn={SuggestedEttnNo}, SuggestedTotal={SuggestedTotal:F2}, TenantId={TenantId}",
            msg.OrderId, msg.SuggestedEttnNo, msg.SuggestedTotal, tenantId);

        try
        {
            // Record the draft event as a notification for accounting team review
            var notification = NotificationLog.Create(
                tenantId,
                DomainChannel.Push,
                "accounting",
                "AI E-Invoice Draft",
                $"AI e-fatura taslagi olusturuldu — OrderId: {msg.OrderId}, " +
                $"ETTN: {msg.SuggestedEttnNo}, Tutar: {msg.SuggestedTotal:F2} TRY. " +
                $"Muhasebe onay bekliyor.");
            notification.MarkAsSent();

            await _notificationLogRepository.AddAsync(notification, context.CancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "[MESA Consumer] AI e-fatura taslagi NotificationLog olarak kaydedildi: " +
                "NotificationId={NotificationId}, OrderId={OrderId}",
                notification.Id, msg.OrderId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "[MESA Consumer] AI e-fatura taslagi islenirken hata: OrderId={OrderId}",
                msg.OrderId);
            throw; // MassTransit retry policy
        }

        _monitor.RecordConsume("ai.einvoice.draft.generated");
    }
}
