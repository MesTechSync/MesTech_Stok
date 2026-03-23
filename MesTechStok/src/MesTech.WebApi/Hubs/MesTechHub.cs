using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MesTech.WebApi.Hubs;

/// <summary>
/// MesTech real-time bildirim hub'i.
/// Tenant bazli grup yonetimi ve event broadcast destegi.
/// Events: OrderReceived, StockAlert, SyncComplete, InvoiceReady, WebhookReceived,
///         ReportReady, ReconciliationDone, FulfillmentAlert, NotificationPush.
/// </summary>
[Authorize]
public class MesTechHub : Hub
{
    private readonly ILogger<MesTechHub> _logger;

    public MesTechHub(ILogger<MesTechHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Client'i tenant grubuna ekler. Tenant bazli event broadcast icin kullanilir.
    /// </summary>
    public async Task JoinTenantGroup(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            return;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant-{tenantId}");

        _logger.LogInformation(
            "SignalR client joined tenant group: connectionId={ConnectionId}, tenantId={TenantId}",
            Context.ConnectionId, tenantId);
    }

    /// <summary>
    /// Client'i import ilerleme grubuna ekler. Import progress event'leri alir.
    /// </summary>
    public async Task JoinImportGroup(string importId)
    {
        if (string.IsNullOrWhiteSpace(importId))
            return;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"import-{importId}");

        _logger.LogInformation(
            "SignalR client joined import group: connectionId={ConnectionId}, importId={ImportId}",
            Context.ConnectionId, importId);
    }

    /// <summary>
    /// Client'i import ilerleme grubundan cikarir.
    /// </summary>
    public async Task LeaveImportGroup(string importId)
    {
        if (string.IsNullOrWhiteSpace(importId))
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"import-{importId}");

        _logger.LogInformation(
            "SignalR client left import group: connectionId={ConnectionId}, importId={ImportId}",
            Context.ConnectionId, importId);
    }

    /// <summary>
    /// Client'i tenant grubundan cikarir.
    /// </summary>
    public async Task LeaveTenantGroup(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant-{tenantId}");

        _logger.LogInformation(
            "SignalR client left tenant group: connectionId={ConnectionId}, tenantId={TenantId}",
            Context.ConnectionId, tenantId);
    }

    // ─── V5 SERVER-TO-CLIENT EVENT METHODS ───

    /// <summary>
    /// Rapor hazır bildirimi — tenant grubuna broadcast.
    /// Client event: "ReportReady" → { reportType, reportUrl, generatedAt }
    /// </summary>
    public static async Task NotifyReportReady(
        IHubContext<MesTechHub> hubContext,
        string tenantId,
        string reportType,
        string reportUrl)
    {
        await hubContext.Clients.Group($"tenant-{tenantId}").SendAsync("ReportReady", new
        {
            reportType,
            reportUrl,
            generatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// ERP mutabakat tamamlandı bildirimi.
    /// Client event: "ReconciliationDone" → { erpProvider, matched, unmatched, generatedAt }
    /// </summary>
    public static async Task NotifyReconciliationDone(
        IHubContext<MesTechHub> hubContext,
        string tenantId,
        string erpProvider,
        int matchedCount,
        int unmatchedCount)
    {
        await hubContext.Clients.Group($"tenant-{tenantId}").SendAsync("ReconciliationDone", new
        {
            erpProvider,
            matchedCount,
            unmatchedCount,
            generatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Fulfillment stok uyarısı bildirimi.
    /// Client event: "FulfillmentAlert" → { center, alertType, message, timestamp }
    /// </summary>
    public static async Task NotifyFulfillmentAlert(
        IHubContext<MesTechHub> hubContext,
        string tenantId,
        string center,
        string alertType,
        string message)
    {
        await hubContext.Clients.Group($"tenant-{tenantId}").SendAsync("FulfillmentAlert", new
        {
            center,
            alertType,
            message,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// InApp bildirim push — tenant grubundaki tum client'lere.
    /// Client event: "NotificationPush" → { title, message, category, actionUrl, timestamp }
    /// </summary>
    public static async Task PushNotification(
        IHubContext<MesTechHub> hubContext,
        string tenantId,
        string title,
        string message,
        string category,
        string? actionUrl = null)
    {
        await hubContext.Clients.Group($"tenant-{tenantId}").SendAsync("NotificationPush", new
        {
            title,
            message,
            category,
            actionUrl,
            timestamp = DateTime.UtcNow
        });
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "SignalR client connected: connectionId={ConnectionId}",
            Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "SignalR client disconnected: connectionId={ConnectionId}, error={Error}",
            Context.ConnectionId, exception?.Message);

        await base.OnDisconnectedAsync(exception);
    }
}
