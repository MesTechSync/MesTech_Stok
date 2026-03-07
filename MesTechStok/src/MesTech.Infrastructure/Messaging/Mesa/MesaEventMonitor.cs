using System.Collections.Concurrent;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Messaging.Mesa;

/// <summary>
/// Thread-safe MESA event istatistik servisi.
/// ConcurrentDictionary ile lock-free sayac.
/// Singleton olarak register edilir — uygulama boyunca tek instance.
/// </summary>
public class MesaEventMonitor : IMesaEventMonitor
{
    private readonly ConcurrentDictionary<string, MutableEventCounter> _counters = new();
    private readonly DateTime _startedAt = DateTime.UtcNow;

    public void RecordPublish(string eventType)
    {
        var counter = _counters.GetOrAdd(eventType, _ => new MutableEventCounter());
        Interlocked.Increment(ref counter.Published);
        counter.LastPublishAt = DateTime.UtcNow;
    }

    public void RecordConsume(string eventType)
    {
        var counter = _counters.GetOrAdd(eventType, _ => new MutableEventCounter());
        Interlocked.Increment(ref counter.Consumed);
        counter.LastConsumeAt = DateTime.UtcNow;
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
            kv => new EventCounter(
                Interlocked.Read(ref kv.Value.Published),
                Interlocked.Read(ref kv.Value.Consumed),
                Interlocked.Read(ref kv.Value.Errors),
                kv.Value.LastPublishAt,
                kv.Value.LastConsumeAt));

        var uptimeSeconds = (long)(DateTime.UtcNow - _startedAt).TotalSeconds;

        return new MesaMonitorStatus("active", events, uptimeSeconds);
    }

    private class MutableEventCounter
    {
        public long Published;
        public long Consumed;
        public long Errors;
        public DateTime? LastPublishAt;
        public DateTime? LastConsumeAt;
    }
}
