using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Ürün fiyatı değiştiğinde bildirim oluşturur.
/// PriceChangedEvent → NotificationLog (fiyat değişikliği takibi).
/// </summary>
public interface IPriceChangedNotificationHandler
{
    Task HandleAsync(
        Guid productId, Guid tenantId, string sku,
        decimal oldPrice, decimal newPrice,
        CancellationToken ct);
}

public sealed class PriceChangedNotificationHandler : IPriceChangedNotificationHandler
{
    private readonly INotificationLogRepository _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PriceChangedNotificationHandler> _logger;

    public PriceChangedNotificationHandler(
        INotificationLogRepository notificationRepo,
        IUnitOfWork uow,
        ILogger<PriceChangedNotificationHandler> logger)
    {
        _notificationRepo = notificationRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid productId, Guid tenantId, string sku,
        decimal oldPrice, decimal newPrice,
        CancellationToken ct)
    {
        var delta = newPrice - oldPrice;
        var direction = delta > 0 ? "artış" : "düşüş";

        _logger.LogInformation(
            "PriceChanged → fiyat bildirimi. SKU={SKU}, Eski={Old}, Yeni={New}, Değişim={Delta}",
            sku, oldPrice, newPrice, delta);

        var content = $"Fiyat değişikliği: {sku} — {oldPrice:C2} → {newPrice:C2} ({direction}: {Math.Abs(delta):C2})";

        var notification = NotificationLog.Create(
            tenantId,
            Domain.Enums.NotificationChannel.Push,
            "dashboard",
            "PriceChanged",
            content);

        await _notificationRepo.AddAsync(notification, ct).ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
