using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Çeşitli yaşam döngüsü bildirimleri — küçük event'ler tek handler'da.
/// BuyboxLostEvent, CashTransactionRecordedEvent, CustomerCreatedEvent,
/// DailySummaryGeneratedEvent, DocumentUploadedEvent, OnboardingCompletedEvent,
/// NotificationSettingsUpdatedEvent, PlatformMessageReceivedEvent,
/// PlatformNotificationFailedEvent, ReturnResolvedEvent,
/// ShipmentCostRecordedEvent, StaleOrderDetectedEvent,
/// SupplierFeedSyncedEvent, SyncRequestedEvent,
/// TaskCompletedEvent, TaskOverdueEvent,
/// LeaveApprovedEvent, LeaveRejectedEvent → NotificationLog.
/// </summary>
public interface IMiscNotificationHandler
{
    Task HandleAsync(Guid tenantId, string eventType, string content, CancellationToken ct);
}

public sealed class MiscNotificationHandler : IMiscNotificationHandler
{
    private readonly INotificationLogRepository _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<MiscNotificationHandler> _logger;

    public MiscNotificationHandler(
        INotificationLogRepository notificationRepo,
        IUnitOfWork uow,
        ILogger<MiscNotificationHandler> logger)
    {
        _notificationRepo = notificationRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(Guid tenantId, string eventType, string content, CancellationToken ct)
    {
        _logger.LogInformation("{EventType} → bildirim oluşturuluyor. TenantId={TenantId}", eventType, tenantId);

        var notification = NotificationLog.Create(
            tenantId,
            Domain.Enums.NotificationChannel.Push,
            "dashboard",
            eventType,
            content);

        await _notificationRepo.AddAsync(notification, ct).ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
