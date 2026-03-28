using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Sipariş kargoya verildiğinde bildirim oluşturur.
/// OrderShippedEvent → NotificationLog (kargo takip).
/// </summary>
public interface IOrderShippedNotificationHandler
{
    Task HandleAsync(
        Guid orderId, Guid tenantId, string trackingNumber,
        CargoProvider cargoProvider, CancellationToken ct);
}

public sealed class OrderShippedNotificationHandler : IOrderShippedNotificationHandler
{
    private readonly INotificationLogRepository _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<OrderShippedNotificationHandler> _logger;

    public OrderShippedNotificationHandler(
        INotificationLogRepository notificationRepo,
        IUnitOfWork uow,
        ILogger<OrderShippedNotificationHandler> logger)
    {
        _notificationRepo = notificationRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid orderId, Guid tenantId, string trackingNumber,
        CargoProvider cargoProvider, CancellationToken ct)
    {
        _logger.LogInformation(
            "OrderShipped → bildirim oluşturuluyor. OrderId={OrderId}, Tracking={Tracking}, Cargo={Cargo}",
            orderId, trackingNumber, cargoProvider);

        var content = $"Sipariş kargoya verildi — Takip: {trackingNumber}, Kargo: {cargoProvider}";

        var notification = NotificationLog.Create(
            tenantId,
            Domain.Enums.NotificationChannel.Push,
            "dashboard",
            "OrderShipped",
            content);

        await _notificationRepo.AddAsync(notification, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
