using MassTransit;
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
public class AiEInvoiceDraftGeneratedConsumer : IConsumer<AiEInvoiceDraftGeneratedIntegrationEvent>
{
    private readonly INotificationLogRepository _notificationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<AiEInvoiceDraftGeneratedConsumer> _logger;

    public AiEInvoiceDraftGeneratedConsumer(
        INotificationLogRepository notificationLogRepository,
        IUnitOfWork unitOfWork,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<AiEInvoiceDraftGeneratedConsumer> logger)
    {
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

            await _notificationLogRepository.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "[MESA Consumer] AI e-fatura taslagi NotificationLog olarak kaydedildi: " +
                "NotificationId={NotificationId}, OrderId={OrderId}",
                notification.Id, msg.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MESA Consumer] AI e-fatura taslagi islenirken hata: OrderId={OrderId}",
                msg.OrderId);
            throw; // MassTransit retry policy
        }

        _monitor.RecordConsume("ai.einvoice.draft.generated");
    }
}
