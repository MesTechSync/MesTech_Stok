namespace MesTech.Application.Interfaces;

/// <summary>
/// Canli dashboard'a realtime bildirim gondermek icin arayuz.
/// WebSocket uzerinden bagli tum istemcilere broadcast yapar.
/// </summary>
public interface IDashboardNotifier
{
    Task NotifySyncStatusAsync(string platform, string status, int progress, int total, CancellationToken ct = default);
    Task NotifyLowStockAsync(string sku, string productName, int currentStock, int minimumStock, CancellationToken ct = default);
    Task NotifyNewOrderAsync(string platform, string orderId, decimal total, int itemCount, CancellationToken ct = default);
    Task NotifyInvoiceGeneratedAsync(string invoiceNumber, string customerName, decimal total, CancellationToken ct = default);
    Task NotifyReturnCreatedAsync(string platform, string returnId, string orderId, string reason, CancellationToken ct = default);
    Task NotifyBuyboxLostAsync(Guid tenantId, string sku, decimal currentPrice, decimal competitorPrice, string competitorName, CancellationToken ct = default);
    Task NotifyPriceAutoUpdatedAsync(Guid tenantId, string sku, decimal oldPrice, decimal newPrice, string strategy, CancellationToken ct = default);
    Task NotifyPriceCycleDoneAsync(Guid tenantId, int updated, int skipped, int tenantCount, CancellationToken ct = default);
}
