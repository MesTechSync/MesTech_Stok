using System.Collections.Concurrent;
using MesTech.Infrastructure.Messaging.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging;

/// <summary>
/// Ring buffer (son 100 event) + SignalR broadcast.
/// Consumer'lar islemi sonrasi bu servisi cagirir.
/// </summary>
public class MesaEventBroadcastService
{
    private readonly IHubContext<MesaEventHub> _hubContext;
    private readonly ConcurrentQueue<MesaEventMessage> _buffer = new();
    private readonly ILogger<MesaEventBroadcastService> _logger;
    private const int MaxBufferSize = 100;

    public MesaEventBroadcastService(
        IHubContext<MesaEventHub> hubContext,
        ILogger<MesaEventBroadcastService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyAsync(MesaEventMessage message)
    {
        _buffer.Enqueue(message);
        while (_buffer.Count > MaxBufferSize)
            _buffer.TryDequeue(out _);

        try
        {
            await _hubContext.Clients.All.SendAsync("MesaEvent", message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast MESA event via SignalR");
        }
    }

    public IReadOnlyList<MesaEventMessage> GetRecentEvents() =>
        _buffer.ToArray().ToList().AsReadOnly();

    public DateTimeOffset? GetLastEventTimestamp() =>
        _buffer.TryPeek(out _) ? _buffer.Last().Timestamp : null;
}
