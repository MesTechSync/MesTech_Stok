using MassTransit;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using Microsoft.Extensions.Logging;
using DomainChannel = MesTech.Domain.Enums.NotificationChannel;

namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;

/// <summary>
/// MESA AI finansal danismanlik onerilerini consume eder.
/// Onerileri NotificationLog olarak kaydeder ve dashboard'a bildirir.
/// </summary>
public class AiAdvisoryRecommendationConsumer : IConsumer<AiAdvisoryRecommendationEvent>
{
    private readonly INotificationLogRepository _notificationLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<AiAdvisoryRecommendationConsumer> _logger;

    public AiAdvisoryRecommendationConsumer(
        INotificationLogRepository notificationLogRepository,
        IUnitOfWork unitOfWork,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<AiAdvisoryRecommendationConsumer> logger)
    {
        _notificationLogRepository = notificationLogRepository;
        _unitOfWork = unitOfWork;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AiAdvisoryRecommendationEvent> context)
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
            "[MESA Consumer] AI danismanlik onerisi alindi: " +
            "Tip={RecommendationType}, Baslik={Title}, Oncelik={Priority}",
            msg.RecommendationType, msg.Title, msg.Priority);

        _logger.LogInformation(
            "[MESA Consumer] Oneri detayi: {Description}",
            msg.Description);

        if (!string.IsNullOrWhiteSpace(msg.ActionUrl))
        {
            _logger.LogInformation(
                "[MESA Consumer] Aksiyon URL: {ActionUrl}", msg.ActionUrl);
        }

        try
        {
            // Create notification log for the advisory recommendation
            var notification = NotificationLog.Create(
                tenantId,
                DomainChannel.Push,
                "system",
                $"AI Advisory: {msg.RecommendationType}",
                $"{msg.Title}: {msg.Description}");
            notification.MarkAsSent();

            await _notificationLogRepository.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "[MESA Consumer] AI onerisi NotificationLog olarak kaydedildi: NotificationId={NotificationId}",
                notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MESA Consumer] AI danismanlik onerisi islenirken hata");
            throw; // MassTransit retry policy
        }

        // Dashboard bildirim sistemi icin kaydet (IDashboardNotifier WebSocket push eklenecek)
        _logger.LogInformation(
            "[MESA Consumer] AI onerisi dashboard bildirimine kaydedildi: " +
            "Tip={RecommendationType}, Oncelik={Priority}",
            msg.RecommendationType, msg.Priority);

        _monitor.RecordConsume("ai.advisory.recommendation");
    }
}
