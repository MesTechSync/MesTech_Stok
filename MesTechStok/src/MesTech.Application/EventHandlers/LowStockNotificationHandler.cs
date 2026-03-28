using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Stok minimum seviyenin altına düştüğünde uyarı bildirimi oluşturur.
/// LowStockDetectedEvent → NotificationLog (stok azalma uyarısı).
/// </summary>
public interface ILowStockNotificationHandler
{
    Task HandleAsync(
        Guid productId, Guid tenantId, string sku,
        int currentStock, int minimumStock,
        CancellationToken ct);
}

public sealed class LowStockNotificationHandler : ILowStockNotificationHandler
{
    private readonly INotificationLogRepository _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<LowStockNotificationHandler> _logger;

    public LowStockNotificationHandler(
        INotificationLogRepository notificationRepo,
        IUnitOfWork uow,
        ILogger<LowStockNotificationHandler> logger)
    {
        _notificationRepo = notificationRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid productId, Guid tenantId, string sku,
        int currentStock, int minimumStock,
        CancellationToken ct)
    {
        _logger.LogWarning(
            "LowStockDetected → uyarı bildirimi oluşturuluyor. SKU={SKU}, Mevcut={Current}, Minimum={Minimum}, TenantId={TenantId}",
            sku, currentStock, minimumStock, tenantId);

        var content = $"STOK UYARISI: {sku} ürününde stok kritik seviyede. " +
                      $"Mevcut: {currentStock}, Minimum: {minimumStock}. Tedarik gerekli.";

        var notification = NotificationLog.Create(
            tenantId,
            Domain.Enums.NotificationChannel.Push,
            "dashboard",
            "LowStockAlert",
            content);

        await _notificationRepo.AddAsync(notification, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Stok uyarısı bildirimi oluşturuldu — SKU={SKU}, ProductId={ProductId}",
            sku, productId);
    }
}
