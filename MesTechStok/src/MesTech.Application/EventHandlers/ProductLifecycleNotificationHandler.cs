using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Ürün yaşam döngüsü bildirimleri — Updated/Activated/Deactivated.
/// Tek handler, çoklu event pattern.
/// </summary>
public interface IProductLifecycleNotificationHandler
{
    Task HandleUpdatedAsync(Guid productId, Guid tenantId, string sku, CancellationToken ct);
    Task HandleActivatedAsync(Guid productId, Guid tenantId, string sku, CancellationToken ct);
    Task HandleDeactivatedAsync(Guid productId, Guid tenantId, string sku, string reason, CancellationToken ct);
}

public sealed class ProductLifecycleNotificationHandler : IProductLifecycleNotificationHandler
{
    private readonly INotificationLogRepository _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProductLifecycleNotificationHandler> _logger;

    public ProductLifecycleNotificationHandler(
        INotificationLogRepository notificationRepo,
        IUnitOfWork uow,
        ILogger<ProductLifecycleNotificationHandler> logger)
    {
        _notificationRepo = notificationRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleUpdatedAsync(Guid productId, Guid tenantId, string sku, CancellationToken ct)
    {
        _logger.LogInformation("ProductUpdated → bildirim. SKU={SKU}", sku);
        await CreateNotificationAsync(tenantId, "ProductUpdated",
            $"Ürün güncellendi — SKU: {sku}", ct);
    }

    public async Task HandleActivatedAsync(Guid productId, Guid tenantId, string sku, CancellationToken ct)
    {
        _logger.LogInformation("ProductActivated → bildirim. SKU={SKU}", sku);
        await CreateNotificationAsync(tenantId, "ProductActivated",
            $"Ürün aktifleştirildi — SKU: {sku}", ct);
    }

    public async Task HandleDeactivatedAsync(Guid productId, Guid tenantId, string sku, string reason, CancellationToken ct)
    {
        _logger.LogWarning("ProductDeactivated → bildirim. SKU={SKU}, Reason={Reason}", sku, reason);
        await CreateNotificationAsync(tenantId, "ProductDeactivated",
            $"Ürün pasifleştirildi — SKU: {sku}, Sebep: {reason}", ct);
    }

    private async Task CreateNotificationAsync(Guid tenantId, string template, string content, CancellationToken ct)
    {
        var notification = NotificationLog.Create(
            tenantId,
            Domain.Enums.NotificationChannel.Push,
            "dashboard",
            template,
            content);

        await _notificationRepo.AddAsync(notification, ct).ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
