using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Platform senkronizasyon hatası oluştuğunda bildirim kaydı oluşturur.
/// SyncErrorOccurredEvent → NotificationLog (sync hatası uyarısı).
/// </summary>
public interface ISyncErrorNotificationHandler
{
    Task HandleAsync(
        Guid tenantId, string platform, string errorType,
        string message, CancellationToken ct);
}

public sealed class SyncErrorNotificationHandler : ISyncErrorNotificationHandler
{
    private readonly INotificationLogRepository _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SyncErrorNotificationHandler> _logger;

    public SyncErrorNotificationHandler(
        INotificationLogRepository notificationRepo,
        IUnitOfWork uow,
        ILogger<SyncErrorNotificationHandler> logger)
    {
        _notificationRepo = notificationRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid tenantId, string platform, string errorType,
        string message, CancellationToken ct)
    {
        _logger.LogError(
            "SyncError → hata bildirimi. Platform={Platform}, ErrorType={Type}, Message={Msg}",
            platform, errorType, message);

        var content = $"SYNC HATASI: {platform} — {errorType}: {message}";

        var notification = NotificationLog.Create(
            tenantId,
            Domain.Enums.NotificationChannel.Push,
            "dashboard",
            "SyncError",
            content);

        await _notificationRepo.AddAsync(notification, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
