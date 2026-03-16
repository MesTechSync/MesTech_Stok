using MediatR;
using MesTech.Application.Interfaces;

namespace MesTech.Application.Features.Dashboard.Queries.GetPlatformHealth;

/// <summary>
/// Platform saglik isleyicisi.
/// SyncLog tablosundan her platform icin en son senkronizasyon durumunu ve
/// son 24 saatteki hata sayisini hesaplar.
/// </summary>
public class GetPlatformHealthHandler : IRequestHandler<GetPlatformHealthQuery, IReadOnlyList<PlatformHealthDto>>
{
    private readonly ISyncLogRepository _syncLogRepository;

    public GetPlatformHealthHandler(ISyncLogRepository syncLogRepository)
        => _syncLogRepository = syncLogRepository;

    public async Task<IReadOnlyList<PlatformHealthDto>> Handle(
        GetPlatformHealthQuery request, CancellationToken cancellationToken)
    {
        var latestLogs = await _syncLogRepository.GetLatestByPlatformAsync(
            request.TenantId, cancellationToken);

        var since24h = DateTime.UtcNow.AddHours(-24);
        var failedLogs = await _syncLogRepository.GetFailedSinceAsync(
            request.TenantId, since24h, cancellationToken);

        var errorCountByPlatform = failedLogs
            .GroupBy(l => l.PlatformCode, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        // Build result from latest logs per platform
        var platforms = latestLogs
            .GroupBy(l => l.PlatformCode, StringComparer.Ordinal)
            .Select(g =>
            {
                var latest = g.OrderByDescending(l => l.StartedAt).First();
                var errorCount = errorCountByPlatform.GetValueOrDefault(g.Key, 0);

                var status = latest.IsSuccess ? "Healthy" :
                    errorCount >= 5 ? "Critical" : "Warning";

                return new PlatformHealthDto
                {
                    Platform = g.Key,
                    LastSyncAt = latest.CompletedAt ?? latest.StartedAt,
                    Status = status,
                    ErrorCount24h = errorCount
                };
            })
            .OrderBy(p => p.Platform, StringComparer.Ordinal)
            .ToList();

        return platforms.AsReadOnly();
    }
}
