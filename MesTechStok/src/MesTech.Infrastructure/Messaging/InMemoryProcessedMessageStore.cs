using System.Collections.Concurrent;

namespace MesTech.Infrastructure.Messaging;

public sealed class InMemoryProcessedMessageStore : IProcessedMessageStore
{
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _processed = new();
    private readonly TimeSpan _ttl = TimeSpan.FromDays(7);
    private long _lastCleanupTicks = DateTimeOffset.UtcNow.UtcTicks;
    private int _cleanupRunning;

    public Task<bool> IsProcessedAsync(Guid messageId, CancellationToken ct = default)
    {
        CleanupIfNeeded();
        return Task.FromResult(_processed.ContainsKey(messageId));
    }

    public Task MarkProcessedAsync(Guid messageId, string consumerName,
        CancellationToken ct = default)
    {
        _processed.TryAdd(messageId, DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }

    private void CleanupIfNeeded()
    {
        var lastTicks = Interlocked.Read(ref _lastCleanupTicks);
        if (DateTimeOffset.UtcNow.UtcTicks - lastTicks < TimeSpan.FromHours(1).Ticks)
            return;

        // Ensure only one thread runs cleanup at a time
        if (Interlocked.CompareExchange(ref _cleanupRunning, 1, 0) != 0)
            return;

        try
        {
            var cutoff = DateTimeOffset.UtcNow - _ttl;
            foreach (var kvp in _processed)
            {
                if (kvp.Value < cutoff)
                    _processed.TryRemove(kvp.Key, out _);
            }
            Interlocked.Exchange(ref _lastCleanupTicks, DateTimeOffset.UtcNow.UtcTicks);
        }
        finally
        {
            Interlocked.Exchange(ref _cleanupRunning, 0);
        }
    }
}
