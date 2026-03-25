using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Satış fiyatı alış fiyatının altına düştüğünde bildirim tetikler.
/// Notification-only handler — Dashboard'da kırmızı badge gösterilir.
/// Infrastructure MediatR bridge handler bu servisi çağırır.
/// </summary>
public interface IPriceLossEventHandler
{
    Task HandleAsync(Guid productId, string sku, decimal purchasePrice, decimal salePrice, decimal lossPerUnit, Guid tenantId, CancellationToken ct);
}

public sealed class PriceLossDetectedEventHandler : IPriceLossEventHandler
{
    private readonly ILogger<PriceLossDetectedEventHandler> _logger;

    public PriceLossDetectedEventHandler(ILogger<PriceLossDetectedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(
        Guid productId,
        string sku,
        decimal purchasePrice,
        decimal salePrice,
        decimal lossPerUnit,
        Guid tenantId,
        CancellationToken ct)
    {
        _logger.LogWarning(
            "PriceLossDetected → SKU={SKU}, PurchasePrice={PurchasePrice:C}, SalePrice={SalePrice:C}, LossPerUnit={LossPerUnit:C}, ProductId={ProductId}, TenantId={TenantId}",
            sku, purchasePrice, salePrice, lossPerUnit, productId, tenantId);

        // FUTURE: Telegram/Email bildirim entegrasyonu (Dalga 14+)
        // FUTURE: Dashboard kırmızı badge tetikleme

        return Task.CompletedTask;
    }
}
