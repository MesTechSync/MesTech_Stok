using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging;

/// <summary>
/// Ring buffer (son 100 event) for MESA event tracking.
/// Consumer'lar islemi sonrasi bu servisi cagirir.
/// MesaEventHub arsivlendi (2026-03-24, kayitsiz dead code) — broadcast kaldirildi, buffer korundu.
/// </summary>
public sealed class MesaEventBroadcastService
{
    private readonly ConcurrentQueue<MesaEventMessage> _buffer = new();
    private readonly ILogger<MesaEventBroadcastService> _logger;
    private const int MaxBufferSize = 100;

    public MesaEventBroadcastService(
        ILogger<MesaEventBroadcastService> logger)
    {
        _logger = logger;
    }

    public Task NotifyAsync(MesaEventMessage message)
    {
        _buffer.Enqueue(message);
        while (_buffer.Count > MaxBufferSize)
            _buffer.TryDequeue(out _);

        _logger.LogDebug("MESA event buffered: {EventType} ({Direction})",
            message.EventType, message.Direction);

        return Task.CompletedTask;
    }

    public IReadOnlyList<MesaEventMessage> GetRecentEvents() =>
        _buffer.ToArray().ToList().AsReadOnly();

    public DateTimeOffset? GetLastEventTimestamp() =>
        _buffer.TryPeek(out _) ? _buffer.Last().Timestamp : null;
}

/// <summary>
/// MESA event message DTO — ring buffer ve health check tarafindan kullanilir.
/// Eskiden MesaEventHub.cs icerisindeydi, hub arsivlendikten sonra buraya tasindi.
/// </summary>
public record MesaEventMessage
{
    public string EventType { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty; // "Inbound" | "Outbound"
    public string Status { get; init; } = string.Empty;    // "Success" | "Failed" | "Skipped"
    public DateTimeOffset Timestamp { get; init; }
    public string? Details { get; init; }
    public Guid? CorrelationId { get; init; }
}
