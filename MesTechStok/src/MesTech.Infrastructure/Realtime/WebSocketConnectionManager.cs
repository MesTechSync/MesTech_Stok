using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Realtime;

/// <summary>
/// WebSocket baglanti yonetimi. Tum bagli istemcilere broadcast yapar.
/// Thread-safe: ConcurrentDictionary ile baglanti takibi.
/// </summary>
public sealed class WebSocketConnectionManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();
    private readonly ILogger<WebSocketConnectionManager> _logger;

    public WebSocketConnectionManager(ILogger<WebSocketConnectionManager> logger)
    {
        _logger = logger;
    }

    public int ConnectionCount => _connections.Count;

    public string AddConnection(WebSocket socket)
    {
        // FIX-DEV6-TUR3: 8-char hex = 32-bit collision space, birthday paradox risk.
        // Full GUID eliminates collision. TryAdd loop handles the theoretical edge case.
        string id;
        do
        {
            id = Guid.NewGuid().ToString("N");
        } while (!_connections.TryAdd(id, socket));

        _logger.LogInformation("WebSocket baglanti eklendi: {Id} (toplam: {Count})", id, _connections.Count);
        return id;
    }

    public void RemoveConnection(string id)
    {
        _connections.TryRemove(id, out _);
        _logger.LogInformation("WebSocket baglanti silindi: {Id} (toplam: {Count})", id, _connections.Count);
    }

    public async Task BroadcastAsync(DashboardEvent evt, CancellationToken ct = default)
    {
        if (_connections.IsEmpty) return;

        var json = JsonSerializer.Serialize(evt);
        var buffer = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(buffer);

        var deadConnections = new List<string>();

        foreach (var (id, socket) in _connections)
        {
            try
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(segment, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
                }
                else
                {
                    deadConnections.Add(id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WebSocket broadcast hatasi: {Id}", id);
                deadConnections.Add(id);
            }
        }

        foreach (var id in deadConnections)
        {
            RemoveConnection(id);
        }
    }
}
