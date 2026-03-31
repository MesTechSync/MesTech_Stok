using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Gecikmiş sipariş tespit edildiğinde bildirim oluşturur (Zincir 11).
/// StaleOrderDetectedEvent → NotificationLog kaydı (Dashboard uyarı badge).
/// Infrastructure MediatR bridge handler bu servisi çağırır.
/// </summary>
public interface IStaleOrderNotificationHandler
{
    Task HandleAsync(
        Guid orderId, Guid tenantId, string orderNumber,
        PlatformType? platform, TimeSpan elapsedSince,
        CancellationToken ct);
}

public sealed class StaleOrderNotificationHandler : IStaleOrderNotificationHandler
{
    private readonly INotificationLogRepository _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<StaleOrderNotificationHandler> _logger;

    public StaleOrderNotificationHandler(
        INotificationLogRepository notificationRepo,
        IUnitOfWork uow,
        ILogger<StaleOrderNotificationHandler> logger)
    {
        _notificationRepo = notificationRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid orderId, Guid tenantId, string orderNumber,
        PlatformType? platform, TimeSpan elapsedSince,
        CancellationToken ct)
    {
        _logger.LogWarning(
            "StaleOrderDetected → gecikmiş sipariş bildirimi oluşturuluyor. Order={OrderNumber}, Platform={Platform}, Elapsed={ElapsedHours}h, TenantId={TenantId}",
            orderNumber, platform, elapsedSince.TotalHours, tenantId);

        var content = $"GECİKMİŞ SİPARİŞ: {orderNumber} siparişi {elapsedSince.TotalHours:F0} saattir gönderilmedi. " +
                      $"Platform: {platform?.ToString() ?? "Bilinmiyor"}";

        var notification = NotificationLog.Create(
            tenantId,
            Domain.Enums.NotificationChannel.Push,
            "dashboard",
            "StaleOrderAlert",
            content);

        await _notificationRepo.AddAsync(notification, ct).ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Gecikmiş sipariş bildirimi oluşturuldu — OrderId={OrderId}, OrderNumber={OrderNumber}, Elapsed={ElapsedHours}h",
            orderId, orderNumber, elapsedSince.TotalHours);
    }
}
