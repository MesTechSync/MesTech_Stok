using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Yeni ürün oluşturulduğunda bildirim kaydı oluşturur.
/// ProductCreatedEvent → NotificationLog (yeni ürün kaydı).
/// </summary>
public interface IProductCreatedNotificationHandler
{
    Task HandleAsync(
        Guid productId, Guid tenantId, string sku, string name,
        decimal salePrice, CancellationToken ct);
}

public sealed class ProductCreatedNotificationHandler : IProductCreatedNotificationHandler
{
    private readonly INotificationLogRepository _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProductCreatedNotificationHandler> _logger;

    public ProductCreatedNotificationHandler(
        INotificationLogRepository notificationRepo,
        IUnitOfWork uow,
        ILogger<ProductCreatedNotificationHandler> logger)
    {
        _notificationRepo = notificationRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid productId, Guid tenantId, string sku, string name,
        decimal salePrice, CancellationToken ct)
    {
        _logger.LogInformation(
            "ProductCreated → bildirim oluşturuluyor. SKU={SKU}, Name={Name}, Price={Price}",
            sku, name, salePrice);

        var content = $"Yeni ürün eklendi: {name} (SKU: {sku}), Fiyat: {salePrice:C2}";

        var notification = NotificationLog.Create(
            tenantId,
            Domain.Enums.NotificationChannel.Push,
            "dashboard",
            "ProductCreated",
            content);

        await _notificationRepo.AddAsync(notification, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
