namespace MesTech.Application.Interfaces;

/// <summary>
/// Çevrimdışı kuyruğu işler — internet gelince bekleyen operasyonları çalıştırır.
/// Tam implementasyon Dalga 3'te, şimdi sadece kontrat.
/// </summary>
public interface ISyncManager
{
    Task ProcessPendingAsync(CancellationToken ct = default);
    Task<SyncManagerStatus> GetStatusAsync(CancellationToken ct = default);
}

public record SyncManagerStatus(
    int PendingCount,
    int ProcessedCount,
    int FailedCount,
    bool IsProcessing,
    DateTime? LastProcessedAt);
