using System.Collections.Concurrent;

namespace MesTech.Infrastructure.Integration.Health;

/// <summary>
/// In-memory platform health history — stores last 24h of health check results.
/// Thread-safe via ConcurrentDictionary + lock-free circular buffer.
/// Used by AdapterHealthCheckJob (writes) and health dashboard endpoint (reads).
/// </summary>
public sealed class PlatformHealthHistory
{
    private readonly ConcurrentDictionary<string, PlatformHealthRecord> _records = new(StringComparer.OrdinalIgnoreCase);
    private const int MaxHistoryEntries = 96; // 24h × 4 checks/hour (every 15min)

    /// <summary>
    /// Record a health check result for a platform.
    /// </summary>
    public void Record(string platformCode, bool isHealthy, long responseTimeMs)
    {
        _records.AddOrUpdate(
            platformCode,
            _ => new PlatformHealthRecord(platformCode, isHealthy, responseTimeMs),
            (_, existing) =>
            {
                existing.AddEntry(isHealthy, responseTimeMs);
                return existing;
            });
    }

    /// <summary>
    /// Get health summary for all known platforms.
    /// </summary>
    public IReadOnlyList<PlatformHealthSummary> GetAllSummaries()
    {
        return _records.Values.Select(r => r.ToSummary()).ToList();
    }

    /// <summary>
    /// Get health summary for a specific platform.
    /// </summary>
    public PlatformHealthSummary? GetSummary(string platformCode)
    {
        return _records.TryGetValue(platformCode, out var record) ? record.ToSummary() : null;
    }
}

/// <summary>
/// Per-platform health record with circular buffer for 24h history.
/// </summary>
public sealed class PlatformHealthRecord
{
    private readonly string _platformCode;
    private readonly HealthEntry[] _entries;
    private int _index;
    private int _count;
    private readonly object _lock = new();

    public PlatformHealthRecord(string platformCode, bool isHealthy, long responseTimeMs)
    {
        _platformCode = platformCode;
        _entries = new HealthEntry[96]; // 24h × 4/hour
        AddEntry(isHealthy, responseTimeMs);
    }

    public void AddEntry(bool isHealthy, long responseTimeMs)
    {
        lock (_lock)
        {
            _entries[_index] = new HealthEntry(DateTime.UtcNow, isHealthy, responseTimeMs);
            _index = (_index + 1) % _entries.Length;
            if (_count < _entries.Length) _count++;
        }
    }

    public PlatformHealthSummary ToSummary()
    {
        lock (_lock)
        {
            if (_count == 0)
                return new PlatformHealthSummary(_platformCode, DateTime.MinValue, 0m, 0, 0, 0);

            var cutoff = DateTime.UtcNow.AddHours(-24);
            int total = 0, healthy = 0, failed = 0;
            long totalResponseTime = 0;
            DateTime lastCheck = DateTime.MinValue;

            for (int i = 0; i < _count; i++)
            {
                var entry = _entries[i];
                if (entry.CheckedAt < cutoff) continue;

                total++;
                if (entry.IsHealthy) healthy++; else failed++;
                totalResponseTime += entry.ResponseTimeMs;
                if (entry.CheckedAt > lastCheck) lastCheck = entry.CheckedAt;
            }

            var uptimePercent = total > 0 ? Math.Round((decimal)healthy / total * 100, 1) : 0m;
            var avgResponseMs = total > 0 ? totalResponseTime / total : 0;

            return new PlatformHealthSummary(
                _platformCode, lastCheck, uptimePercent, failed, avgResponseMs, total);
        }
    }
}

/// <summary>
/// Platform health summary for dashboard consumption.
/// </summary>
public sealed record PlatformHealthSummary(
    string PlatformCode,
    DateTime LastCheckUtc,
    decimal UptimePercent24h,
    int FailedChecks24h,
    long AvgResponseTimeMs,
    int TotalChecks24h);

internal readonly record struct HealthEntry(DateTime CheckedAt, bool IsHealthy, long ResponseTimeMs);
