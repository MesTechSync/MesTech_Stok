using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Kritik stok alarmında bildirim oluşturur (3 seviye: Low/Critical/OutOfStock).
/// StockCriticalEvent → NotificationLog + MESA OS RabbitMQ event.
/// </summary>
public interface IStockCriticalNotificationHandler
{
    Task HandleAsync(
        Guid productId, Guid tenantId, string sku, string productName,
        int currentStock, int minimumStock, StockAlertLevel level,
        Guid? warehouseId, string? warehouseName,
        CancellationToken ct);
}

public sealed class StockCriticalNotificationHandler : IStockCriticalNotificationHandler
{
    private readonly INotificationLogRepository _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<StockCriticalNotificationHandler> _logger;

    public StockCriticalNotificationHandler(
        INotificationLogRepository notificationRepo,
        IUnitOfWork uow,
        ILogger<StockCriticalNotificationHandler> logger)
    {
        _notificationRepo = notificationRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid productId, Guid tenantId, string sku, string productName,
        int currentStock, int minimumStock, StockAlertLevel level,
        Guid? warehouseId, string? warehouseName,
        CancellationToken ct)
    {
        var severity = level switch
        {
            StockAlertLevel.OutOfStock => "KRİTİK",
            StockAlertLevel.Critical => "YÜKSEK",
            _ => "UYARI"
        };

        _logger.LogWarning(
            "StockCritical [{Severity}] → SKU={SKU}, Product={Name}, Stok={Current}/{Min}, Depo={Warehouse}",
            severity, sku, productName, currentStock, minimumStock, warehouseName ?? "Ana Depo");

        var content = $"[{severity}] STOK ALARMI: {productName} (SKU: {sku}) — " +
                      $"Mevcut: {currentStock}, Minimum: {minimumStock}, " +
                      $"Depo: {warehouseName ?? "Ana Depo"}";

        var channel = level == StockAlertLevel.OutOfStock
            ? Domain.Enums.NotificationChannel.Email
            : Domain.Enums.NotificationChannel.Push;

        var notification = NotificationLog.Create(
            tenantId,
            channel,
            "dashboard",
            "StockCritical",
            content);

        await _notificationRepo.AddAsync(notification, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
