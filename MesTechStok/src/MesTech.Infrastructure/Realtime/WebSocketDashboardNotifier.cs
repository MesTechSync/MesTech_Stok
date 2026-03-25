using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Realtime;

/// <summary>
/// IDashboardNotifier implementasyonu. DashboardEvent olusturup
/// WebSocketConnectionManager uzerinden tum bagli istemcilere broadcast yapar.
/// </summary>
public sealed class WebSocketDashboardNotifier : IDashboardNotifier
{
    private readonly WebSocketConnectionManager _connectionManager;

    public WebSocketDashboardNotifier(WebSocketConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public Task NotifySyncStatusAsync(string platform, string status, int progress, int total, CancellationToken ct = default)
    {
        return _connectionManager.BroadcastAsync(new DashboardEvent
        {
            EventType = DashboardEventType.SyncStatus,
            Platform = platform,
            Data = new { status, progress, total }
        }, ct);
    }

    public Task NotifyLowStockAsync(string sku, string productName, int currentStock, int minimumStock, CancellationToken ct = default)
    {
        return _connectionManager.BroadcastAsync(new DashboardEvent
        {
            EventType = DashboardEventType.StockLow,
            Data = new { sku, productName, current = currentStock, minimum = minimumStock }
        }, ct);
    }

    public Task NotifyNewOrderAsync(string platform, string orderId, decimal total, int itemCount, CancellationToken ct = default)
    {
        return _connectionManager.BroadcastAsync(new DashboardEvent
        {
            EventType = DashboardEventType.OrderNew,
            Platform = platform,
            Data = new { orderId, total, itemCount }
        }, ct);
    }

    public Task NotifyInvoiceGeneratedAsync(string invoiceNumber, string customerName, decimal total, CancellationToken ct = default)
    {
        return _connectionManager.BroadcastAsync(new DashboardEvent
        {
            EventType = DashboardEventType.InvoiceGenerated,
            Data = new { invoiceNumber, customerName, total }
        }, ct);
    }

    public Task NotifyReturnCreatedAsync(string platform, string returnId, string orderId, string reason, CancellationToken ct = default)
    {
        return _connectionManager.BroadcastAsync(new DashboardEvent
        {
            EventType = DashboardEventType.ReturnCreated,
            Platform = platform,
            Data = new { returnId, orderId, reason }
        }, ct);
    }
}
