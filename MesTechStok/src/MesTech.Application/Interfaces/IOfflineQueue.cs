namespace MesTech.Application.Interfaces;

/// <summary>
/// Çevrimdışı mod — internet yokken yapılan işlemleri kuyruğa alır.
/// Tam implementasyon Dalga 3'te, şimdi sadece kontrat.
/// </summary>
public interface IOfflineQueue
{
    Task EnqueueAsync(string channel, string payload, CancellationToken ct = default);
    Task<IReadOnlyList<OfflineQueueEntry>> GetPendingAsync(int maxItems = 50, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid entryId, CancellationToken ct = default);
    Task MarkFailedAsync(Guid entryId, string error, CancellationToken ct = default);
    Task<int> GetPendingCountAsync(CancellationToken ct = default);
}

public record OfflineQueueEntry(
    Guid Id,
    string Channel,
    string Payload,
    DateTime CreatedAt,
    int RetryCount);
