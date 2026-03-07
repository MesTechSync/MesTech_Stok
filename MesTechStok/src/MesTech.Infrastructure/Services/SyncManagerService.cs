using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// Sync manager — iskelet implementasyon (Dalga 3'te tamamlanacak).
/// </summary>
public class SyncManagerService : ISyncManager
{
    private readonly ILogger<SyncManagerService> _logger;

    public SyncManagerService(ILogger<SyncManagerService> logger)
    {
        _logger = logger;
    }

    public Task ProcessPendingAsync(CancellationToken ct = default)
    {
        _logger.LogWarning("SyncManager.ProcessPending cagrildi — henuz implementasyon yok (Dalga 3)");
        return Task.CompletedTask;
    }

    public Task<SyncManagerStatus> GetStatusAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new SyncManagerStatus(
            PendingCount: 0,
            ProcessedCount: 0,
            FailedCount: 0,
            IsProcessing: false,
            LastProcessedAt: null));
    }
}
