using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Sipariş tamamlandığında bildirim oluşturur.
/// OrderCompletedEvent → NotificationLog (sipariş teslim edildi).
/// </summary>
public interface IOrderCompletedNotificationHandler
{
    Task HandleAsync(
        Guid orderId, Guid tenantId, string orderNumber,
        decimal totalAmount, string? customerName,
        CancellationToken ct);
}

public sealed class OrderCompletedNotificationHandler : IOrderCompletedNotificationHandler
{
    private readonly INotificationLogRepository _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<OrderCompletedNotificationHandler> _logger;

    public OrderCompletedNotificationHandler(
        INotificationLogRepository notificationRepo,
        IUnitOfWork uow,
        ILogger<OrderCompletedNotificationHandler> logger)
    {
        _notificationRepo = notificationRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid orderId, Guid tenantId, string orderNumber,
        decimal totalAmount, string? customerName,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "OrderCompleted → bildirim oluşturuluyor. Order={OrderNumber}, Customer={Customer}, Total={Total}",
            orderNumber, customerName, totalAmount);

        var content = $"Sipariş #{orderNumber} tamamlandı — Müşteri: {customerName ?? "Bilinmiyor"}, Tutar: {totalAmount:C2}";

        var notification = NotificationLog.Create(
            tenantId,
            Domain.Enums.NotificationChannel.Push,
            "dashboard",
            "OrderCompleted",
            content);

        await _notificationRepo.AddAsync(notification, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
