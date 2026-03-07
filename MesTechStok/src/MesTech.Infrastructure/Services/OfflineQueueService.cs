using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// Çevrimdışı kuyruk — iskelet implementasyon (Dalga 3'te tamamlanacak).
/// Şu an NotImplementedException fırlatır.
/// </summary>
public class OfflineQueueService : IOfflineQueue
{
    private readonly ILogger<OfflineQueueService> _logger;

    public OfflineQueueService(ILogger<OfflineQueueService> logger)
    {
        _logger = logger;
    }

    public Task EnqueueAsync(string channel, string payload, CancellationToken ct = default)
    {
        _logger.LogWarning("OfflineQueue.Enqueue cagrildi — henuz implementasyon yok (Dalga 3)");
        throw new NotImplementedException("OfflineQueue Dalga 3'te tamamlanacak.");
    }

    public Task<IReadOnlyList<OfflineQueueEntry>> GetPendingAsync(int maxItems = 50, CancellationToken ct = default)
    {
        throw new NotImplementedException("OfflineQueue Dalga 3'te tamamlanacak.");
    }

    public Task MarkProcessedAsync(Guid entryId, CancellationToken ct = default)
    {
        throw new NotImplementedException("OfflineQueue Dalga 3'te tamamlanacak.");
    }

    public Task MarkFailedAsync(Guid entryId, string error, CancellationToken ct = default)
    {
        throw new NotImplementedException("OfflineQueue Dalga 3'te tamamlanacak.");
    }

    public Task<int> GetPendingCountAsync(CancellationToken ct = default)
    {
        return Task.FromResult(0);
    }
}
