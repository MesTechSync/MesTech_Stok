using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// İade talebi oluşturulduğunda bildirim kaydı oluşturur.
/// ReturnCreatedEvent → NotificationLog (iade talebi alındı).
/// </summary>
public interface IReturnCreatedNotificationHandler
{
    Task HandleAsync(
        Guid returnRequestId, Guid tenantId, Guid orderId,
        PlatformType platform, ReturnReason reason,
        CancellationToken ct);
}

public sealed class ReturnCreatedNotificationHandler : IReturnCreatedNotificationHandler
{
    private readonly INotificationLogRepository _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ReturnCreatedNotificationHandler> _logger;

    public ReturnCreatedNotificationHandler(
        INotificationLogRepository notificationRepo,
        IUnitOfWork uow,
        ILogger<ReturnCreatedNotificationHandler> logger)
    {
        _notificationRepo = notificationRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid returnRequestId, Guid tenantId, Guid orderId,
        PlatformType platform, ReturnReason reason,
        CancellationToken ct)
    {
        _logger.LogWarning(
            "ReturnCreated → iade bildirimi oluşturuluyor. ReturnId={ReturnId}, Platform={Platform}, Reason={Reason}",
            returnRequestId, platform, reason);

        var content = $"İade talebi alındı — Platform: {platform}, Sebep: {reason}, Sipariş: {orderId}";

        var notification = NotificationLog.Create(
            tenantId,
            Domain.Enums.NotificationChannel.Push,
            "dashboard",
            "ReturnCreated",
            content);

        await _notificationRepo.AddAsync(notification, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
