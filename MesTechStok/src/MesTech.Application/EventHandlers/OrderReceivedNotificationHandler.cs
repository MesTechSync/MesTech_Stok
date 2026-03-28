using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Platformdan yeni sipariş alındığında bildirim oluşturur.
/// OrderReceivedEvent → NotificationLog.
/// </summary>
public interface IOrderReceivedNotificationHandler
{
    Task HandleAsync(
        Guid orderId, Guid tenantId, string platformCode,
        string platformOrderId, decimal totalAmount,
        CancellationToken ct);
}

public sealed class OrderReceivedNotificationHandler : IOrderReceivedNotificationHandler
{
    private readonly INotificationLogRepository _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<OrderReceivedNotificationHandler> _logger;

    public OrderReceivedNotificationHandler(
        INotificationLogRepository notificationRepo,
        IUnitOfWork uow,
        ILogger<OrderReceivedNotificationHandler> logger)
    {
        _notificationRepo = notificationRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid orderId, Guid tenantId, string platformCode,
        string platformOrderId, decimal totalAmount,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "OrderReceived → bildirim. Platform={Platform}, PlatformOrderId={POId}, Total={Total}",
            platformCode, platformOrderId, totalAmount);

        var content = $"Yeni sipariş alındı — {platformCode} #{platformOrderId}, Tutar: {totalAmount:C2}";

        var notification = NotificationLog.Create(
            tenantId,
            Domain.Enums.NotificationChannel.Push,
            "dashboard",
            "OrderReceived",
            content);

        await _notificationRepo.AddAsync(notification, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
