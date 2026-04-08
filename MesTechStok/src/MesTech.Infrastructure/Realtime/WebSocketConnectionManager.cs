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
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _sendLocks = new();
    private readonly ILogger<WebSocketConnectionManager> _logger;
    private const int MaxConnections = 500; // DoS koruması — sınırsız bağlantı önleme

    public WebSocketConnectionManager(ILogger<WebSocketConnectionManager> logger)
    {
        _logger = logger;
    }

    public int ConnectionCount => _connections.Count;

    public string AddConnection(WebSocket socket)
    {
        if (_connections.Count >= MaxConnections)
        {
            _logger.LogWarning("WebSocket MaxConnections ({Max}) reached — rejecting new connection", MaxConnections);
            throw new InvalidOperationException($"Maximum WebSocket connections ({MaxConnections}) exceeded");
        }

        // FIX-DEV6-TUR3: 8-char hex = 32-bit collision space, birthday paradox risk.
        // Full GUID eliminates collision. TryAdd loop handles the theoretical edge case.
        string id;
        do
        {
            id = Guid.NewGuid().ToString("N");
        } while (!_connections.TryAdd(id, socket));

        _sendLocks.TryAdd(id, new SemaphoreSlim(1, 1));
        _logger.LogInformation("WebSocket baglanti eklendi: {Id} (toplam: {Count})", id, _connections.Count);
        return id;
    }

    public void RemoveConnection(string id)
    {
        _connections.TryRemove(id, out _);
        if (_sendLocks.TryRemove(id, out var semaphore))
            semaphore.Dispose();
        _logger.LogInformation("WebSocket baglanti silindi: {Id} (toplam: {Count})", id, _connections.Count);
    }

    public async Task BroadcastAsync(DashboardEvent evt, CancellationToken ct = default)
    {
        if (_connections.IsEmpty) return;

        var json = JsonSerializer.Serialize(evt);
        var buffer = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(buffer);

        var deadConnections = new List<string>();

        // FIX-DEV6-TUR2B: Snapshot prevents InvalidOperationException when
        // RemoveConnection modifies _connections during iteration.
        foreach (var (id, socket) in _connections.ToArray())
        {
            try
            {
                if (socket.State != WebSocketState.Open)
                {
                    deadConnections.Add(id);
                    continue;
                }

                // FIX-DEV6-TUR2: WebSocket.SendAsync is NOT thread-safe per socket.
                // Concurrent BroadcastAsync calls must serialize sends per connection.
                if (!_sendLocks.TryGetValue(id, out var semaphore))
                {
                    deadConnections.Add(id);
                    continue;
                }

                await semaphore.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    if (socket.State == WebSocketState.Open)
                        await socket.SendAsync(segment, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
                    else
                        deadConnections.Add(id);
                }
                finally
                {
                    semaphore.Release();
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
