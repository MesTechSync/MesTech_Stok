using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Satış fiyatı alış fiyatının altına düştüğünde bildirim oluşturur (Zincir 10).
/// PriceLossDetectedEvent → NotificationLog kaydı (Dashboard kırmızı badge).
/// Infrastructure MediatR bridge handler bu servisi çağırır.
/// </summary>
public interface IPriceLossDetectedEventHandler
{
    Task HandleAsync(
        Guid productId, Guid tenantId, string sku,
        decimal purchasePrice, decimal salePrice, decimal lossPerUnit,
        CancellationToken ct);
}

public sealed class PriceLossDetectedEventHandler : IPriceLossDetectedEventHandler
{
    private readonly INotificationLogRepository _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PriceLossDetectedEventHandler> _logger;

    public PriceLossDetectedEventHandler(
        INotificationLogRepository notificationRepo,
        IUnitOfWork uow,
        ILogger<PriceLossDetectedEventHandler> logger)
    {
        _notificationRepo = notificationRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid productId, Guid tenantId, string sku,
        decimal purchasePrice, decimal salePrice, decimal lossPerUnit,
        CancellationToken ct)
    {
        _logger.LogWarning(
            "PriceLossDetected → zarar uyarısı oluşturuluyor. SKU={SKU}, Alış={PurchasePrice}, Satış={SalePrice}, Birim Zarar={LossPerUnit}, TenantId={TenantId}",
            sku, purchasePrice, salePrice, lossPerUnit, tenantId);

        var content = $"ZARAR UYARISI: {sku} ürününde birim zarar tespit edildi. " +
                      $"Alış: {purchasePrice:C2}, Satış: {salePrice:C2}, Birim Zarar: {lossPerUnit:C2}";

        var notification = NotificationLog.Create(
            tenantId,
            Domain.Enums.NotificationChannel.Push,
            "dashboard",
            "PriceLossAlert",
            content);

        await _notificationRepo.AddAsync(notification, ct).ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Zarar uyarısı bildirimi oluşturuldu — SKU={SKU}, ProductId={ProductId}, LossPerUnit={LossPerUnit}",
            sku, productId, lossPerUnit);
    }
}
