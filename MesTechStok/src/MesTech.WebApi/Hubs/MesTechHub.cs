using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MesTech.WebApi.Hubs;

/// <summary>
/// MesTech real-time bildirim hub'i.
/// Tenant bazli grup yonetimi ve event broadcast destegi.
/// Events: OrderReceived, StockAlert, SyncComplete, InvoiceReady, WebhookReceived.
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
