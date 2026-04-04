using System.Collections.Concurrent;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Messaging.Mesa;

/// <summary>
/// Thread-safe MESA event istatistik servisi.
/// ConcurrentDictionary ile lock-free sayac.
/// Singleton olarak register edilir — uygulama boyunca tek instance.
/// </summary>
public sealed class MesaEventMonitor : IMesaEventMonitor
{
    private readonly ConcurrentDictionary<string, MutableEventCounter> _counters = new();
    private readonly DateTime _startedAt = DateTime.UtcNow;

    public void RecordPublish(string eventType)
    {
        var counter = _counters.GetOrAdd(eventType, _ => new MutableEventCounter());
        Interlocked.Increment(ref counter.Published);
        Interlocked.Exchange(ref counter.LastPublishTicks, DateTime.UtcNow.Ticks);
    }

    public void RecordConsume(string eventType)
    {
        var counter = _counters.GetOrAdd(eventType, _ => new MutableEventCounter());
        Interlocked.Increment(ref counter.Consumed);
        Interlocked.Exchange(ref counter.LastConsumeTicks, DateTime.UtcNow.Ticks);
    }

    public void RecordError(string eventType, string errorMessage)
    {
        var counter = _counters.GetOrAdd(eventType, _ => new MutableEventCounter());
        Interlocked.Increment(ref counter.Errors);
    }

    public MesaMonitorStatus GetStatus()
    {
        var events = _counters.ToDictionary(
            kv => kv.Key,
            kv =>
            {
                var pubTicks = Interlocked.Read(ref kv.Value.LastPublishTicks);
                var conTicks = Interlocked.Read(ref kv.Value.LastConsumeTicks);
                return new EventCounter(
                    Interlocked.Read(ref kv.Value.Published),
                    Interlocked.Read(ref kv.Value.Consumed),
                    Interlocked.Read(ref kv.Value.Errors),
                    pubTicks > 0 ? new DateTime(pubTicks, DateTimeKind.Utc) : null,
                    conTicks > 0 ? new DateTime(conTicks, DateTimeKind.Utc) : null);
            });

        var uptimeSeconds = (long)(DateTime.UtcNow - _startedAt).TotalSeconds;

        return new MesaMonitorStatus("active", events, uptimeSeconds);
    }

    private class MutableEventCounter
    {
        public long Published;
        public long Consumed;
        public long Errors;
        public long LastPublishTicks;
        public long LastConsumeTicks;
    }
}
