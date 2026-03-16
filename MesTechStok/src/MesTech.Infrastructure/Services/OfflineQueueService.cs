using System.Collections.Concurrent;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// Cevrimdisi kuyruk — in-memory fallback implementasyon.
/// Dalga 3'te kalici storage (PostgreSQL/Redis) gecisi yapilacak.
/// Su an ConcurrentQueue ile bellek icinde calisir — uygulama restart'inda veri kaybolur.
/// </summary>
public class OfflineQueueService : IOfflineQueue
{
    private readonly ILogger<OfflineQueueService> _logger;
    private readonly ConcurrentDictionary<Guid, OfflineQueueEntry> _queue = new();

    public OfflineQueueService(ILogger<OfflineQueueService> logger)
    {
        _logger = logger;
    }

    public Task EnqueueAsync(string channel, string payload, CancellationToken ct = default)
    {
        var entry = new OfflineQueueEntry(
            Guid.NewGuid(),
            channel,
            payload,
            DateTime.UtcNow,
            RetryCount: 0);

        _queue.TryAdd(entry.Id, entry);
        _logger.LogWarning(
            "OfflineQueue: item enqueued (in-memory fallback). Id={Id}, Channel={Channel}",
            entry.Id, channel);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OfflineQueueEntry>> GetPendingAsync(int maxItems = 50, CancellationToken ct = default)
    {
        var pending = _queue.Values
            .OrderBy(e => e.CreatedAt)
            .Take(maxItems)
            .ToList();

        _logger.LogInformation(
            "OfflineQueue: GetPending returned {Count} items (in-memory fallback)", pending.Count);

        return Task.FromResult<IReadOnlyList<OfflineQueueEntry>>(pending.AsReadOnly());
    }

    public Task MarkProcessedAsync(Guid entryId, CancellationToken ct = default)
    {
        if (_queue.TryRemove(entryId, out _))
        {
            _logger.LogInformation(
                "OfflineQueue: item marked processed and removed. Id={Id}", entryId);
        }
        else
        {
            _logger.LogWarning(
                "OfflineQueue: MarkProcessed — entry not found. Id={Id}", entryId);
        }

        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(Guid entryId, string error, CancellationToken ct = default)
    {
        if (_queue.TryGetValue(entryId, out var existing))
        {
            // Replace with incremented retry count
            var updated = existing with { RetryCount = existing.RetryCount + 1 };
            _queue.TryUpdate(entryId, updated, existing);
            _logger.LogWarning(
                "OfflineQueue: item marked failed (in-memory fallback). Id={Id}, Retry={Retry}, Error={Error}",
                entryId, updated.RetryCount, error);
        }
        else
        {
            _logger.LogWarning(
                "OfflineQueue: MarkFailed — entry not found. Id={Id}", entryId);
        }

        return Task.CompletedTask;
    }

    public Task<int> GetPendingCountAsync(CancellationToken ct = default)
    {
        var count = _queue.Count;
        _logger.LogInformation("OfflineQueue: pending count = {Count} (in-memory fallback)", count);
        return Task.FromResult(count);
    }
}
