using System.Collections.Concurrent;

namespace MesTech.Infrastructure.Messaging;

public class InMemoryProcessedMessageStore : IProcessedMessageStore
{
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _processed = new();
    private readonly TimeSpan _ttl = TimeSpan.FromDays(7);
    private DateTimeOffset _lastCleanup = DateTimeOffset.UtcNow;

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
        if (DateTimeOffset.UtcNow - _lastCleanup < TimeSpan.FromHours(1))
            return;

        var cutoff = DateTimeOffset.UtcNow - _ttl;
        foreach (var kvp in _processed)
        {
            if (kvp.Value < cutoff)
                _processed.TryRemove(kvp.Key, out _);
        }
        _lastCleanup = DateTimeOffset.UtcNow;
    }
}
