using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Integration.Health;

/// <summary>
/// PlatformHealthHistory → IPlatformHealthProvider adapter.
/// Application handler'ları bu servis üzerinden health verisi okur.
/// </summary>
public sealed class PlatformHealthProvider : IPlatformHealthProvider
{
    private readonly PlatformHealthHistory _history;

    public PlatformHealthProvider(PlatformHealthHistory history)
    {
        _history = history ?? throw new ArgumentNullException(nameof(history));
    }

    public PlatformHealthSummaryDto? GetHealthSummary(string platformCode)
    {
        var summary = _history.GetSummary(platformCode);
        return summary is null ? null : ToDto(summary);
    }

    public IReadOnlyList<PlatformHealthSummaryDto> GetAllHealthSummaries()
    {
        return _history.GetAllSummaries()
            .Select(ToDto)
            .ToList();
    }

    private static PlatformHealthSummaryDto ToDto(PlatformHealthSummary s) =>
        new(s.PlatformCode, s.LastCheckUtc, s.UptimePercent24h,
            s.FailedChecks24h, s.AvgResponseTimeMs, s.TotalChecks24h);
}
