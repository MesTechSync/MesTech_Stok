using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MesTech.WebApi.Hubs;

/// <summary>
/// MesTech real-time bildirim hub'i.
/// Tenant bazli grup yonetimi ve event broadcast destegi.
/// Events: OrderReceived, StockAlert, SyncComplete, InvoiceReady, WebhookReceived, NotificationPush.
/// Arsivlenen: ReportReady, ReconciliationDone, FulfillmentAlert (2026-03-24, dead code).
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
    /// G104 FIX: tenantId JWT claim ile dogrulanir — client baska tenant'a katılamaz.
    /// </summary>
    public async Task JoinTenantGroup(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            return;

        var claimTenantId = Context.User?.FindFirst("tenant_id")?.Value
                         ?? Context.User?.FindFirst("tenantId")?.Value;

        if (claimTenantId is null || !string.Equals(claimTenantId, tenantId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "SignalR tenant group REJECTED: connectionId={ConnectionId}, requested={Requested}, claim={Claim}",
                Context.ConnectionId, tenantId, claimTenantId ?? "null");
            throw new HubException("Tenant group access denied: tenant mismatch");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant-{tenantId}").ConfigureAwait(false);

        _logger.LogInformation(
            "SignalR client joined tenant group: connectionId={ConnectionId}, tenantId={TenantId}",
            Context.ConnectionId, tenantId);
    }

    /// <summary>
    /// Client'i import ilerleme grubuna ekler. Import progress event'leri alir.
    /// G104 FIX: Import group'a katilim tenant dogrulamasi ile korunur —
    /// client sadece kendi tenant'inin import'larini izleyebilir.
    /// </summary>
    public async Task JoinImportGroup(string importId)
    {
        if (string.IsNullOrWhiteSpace(importId))
            return;

        var claimTenantId = Context.User?.FindFirst("tenant_id")?.Value
                         ?? Context.User?.FindFirst("tenantId")?.Value;

        if (string.IsNullOrWhiteSpace(claimTenantId))
        {
            _logger.LogWarning(
                "SignalR import group REJECTED: connectionId={ConnectionId}, importId={ImportId} — no tenant claim",
                Context.ConnectionId, importId);
            throw new HubException("Import group access denied: tenant claim missing");
        }

        // Validate importId format — must be valid GUID to prevent group name injection
        if (!Guid.TryParse(importId, out _))
        {
            _logger.LogWarning(
                "SignalR import group REJECTED: connectionId={ConnectionId}, importId={ImportId} — invalid GUID format",
                Context.ConnectionId, importId);
            throw new HubException("Import group access denied: invalid import ID format");
        }

        // Tenant claim doğrulandı + importId validated — import group'a erişim güvenli
        await Groups.AddToGroupAsync(Context.ConnectionId, $"import-{importId}").ConfigureAwait(false);

        _logger.LogInformation(
            "SignalR client joined import group: connectionId={ConnectionId}, importId={ImportId}, tenant={TenantId}",
            Context.ConnectionId, importId, claimTenantId);
    }

    /// <summary>
    /// Client'i import ilerleme grubundan cikarir.
    /// </summary>
    public async Task LeaveImportGroup(string importId)
    {
        if (string.IsNullOrWhiteSpace(importId))
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"import-{importId}").ConfigureAwait(false);

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

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant-{tenantId}").ConfigureAwait(false);

        _logger.LogInformation(
            "SignalR client left tenant group: connectionId={ConnectionId}, tenantId={TenantId}",
            Context.ConnectionId, tenantId);
    }

    // ─── V5 SERVER-TO-CLIENT EVENT METHODS ───
    // Arsivlenen dead-code: NotifyReportReady, NotifyReconciliationDone, NotifyFulfillmentAlert
    // Bkz: Docs/ARSIV/ARSIV_MesTechHub_DeadMethods_2026-03-24.md

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
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Pricing event push — buybox kaybı veya otomatik fiyat güncellemesi.
    /// Client event: "PricingEvent" → { eventType, sku, oldPrice, newPrice, reason, timestamp }
    /// </summary>
    public static async Task PushPricingEvent(
        IHubContext<MesTechHub> hubContext,
        string tenantId,
        string eventType,
        string sku,
        decimal? oldPrice = null,
        decimal? newPrice = null,
        string? reason = null)
    {
        await hubContext.Clients.Group($"tenant-{tenantId}").SendAsync("PricingEvent", new
        {
            eventType,
            sku,
            oldPrice,
            newPrice,
            reason,
            timestamp = DateTime.UtcNow
        }).ConfigureAwait(false);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "SignalR client connected: connectionId={ConnectionId}",
            Context.ConnectionId);

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "SignalR client disconnected: connectionId={ConnectionId}, error={Error}",
            Context.ConnectionId, exception?.Message);

        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }
}
