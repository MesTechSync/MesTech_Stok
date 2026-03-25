using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Health;

/// <summary>
/// Tüm platform adapter'larının bağlantı durumunu kontrol eder.
/// /health/adapters endpoint'i bu servisi kullanır.
/// Her adapter'a GetCategoriesAsync ile hafif bir ping atar.
/// 10sn timeout — yavaş adapter'lar "Timeout" olarak raporlanır.
/// </summary>
public sealed class AdapterHealthService
{
    private readonly IEnumerable<IIntegratorAdapter> _adapters;
    private readonly ILogger<AdapterHealthService> _logger;

    public AdapterHealthService(
        IEnumerable<IIntegratorAdapter> adapters,
        ILogger<AdapterHealthService> logger)
    {
        _adapters = adapters;
        _logger = logger;
    }

    public async Task<AdapterHealthReport> CheckAllAdaptersAsync(CancellationToken ct = default)
    {
        var tasks = _adapters.Select(a => CheckSingleAdapterAsync(a, ct));
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        return new AdapterHealthReport
        {
            CheckedAt = DateTime.UtcNow,
            TotalAdapters = results.Length,
            HealthyCount = results.Count(r => r.IsHealthy),
            UnhealthyCount = results.Count(r => !r.IsHealthy),
            Adapters = results
        };
    }

    private async Task<AdapterHealthResult> CheckSingleAdapterAsync(
        IIntegratorAdapter adapter, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            // Prefer IPingableAdapter.PingAsync (lightweight HEAD) over GetCategoriesAsync (heavy API call)
            if (adapter is IPingableAdapter pingable)
            {
                var isReachable = await pingable.PingAsync(cts.Token).ConfigureAwait(false);
                sw.Stop();
                return new AdapterHealthResult(
                    adapter.PlatformCode, isReachable, sw.ElapsedMilliseconds,
                    isReachable ? "OK — ping reachable" : "Unreachable");
            }

            var categories = await adapter.GetCategoriesAsync(cts.Token).ConfigureAwait(false);
            sw.Stop();

            return new AdapterHealthResult(
                adapter.PlatformCode, true, sw.ElapsedMilliseconds,
                $"OK — {categories.Count} kategori");
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return new AdapterHealthResult(
                adapter.PlatformCode, false, sw.ElapsedMilliseconds,
                "Timeout (10s)");
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Adapter health check failed: {Platform}", adapter.PlatformCode);
            return new AdapterHealthResult(
                adapter.PlatformCode, false, sw.ElapsedMilliseconds,
                $"Error: {ex.Message}");
        }
    }
}

public sealed record AdapterHealthReport
{
    public DateTime CheckedAt { get; init; }
    public int TotalAdapters { get; init; }
    public int HealthyCount { get; init; }
    public int UnhealthyCount { get; init; }
    public IReadOnlyList<AdapterHealthResult> Adapters { get; init; } = Array.Empty<AdapterHealthResult>();
}

public sealed record AdapterHealthResult(
    string PlatformCode, bool IsHealthy, long ResponseTimeMs, string Message);
