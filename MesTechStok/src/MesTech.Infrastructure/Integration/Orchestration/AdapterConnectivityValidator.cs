using System.Diagnostics;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Orchestration;

/// <summary>
/// Validates connectivity for all registered marketplace adapters.
/// Calls IPingableAdapter.PingAsync on each adapter concurrently
/// and returns a consolidated health report.
/// </summary>
public sealed class AdapterConnectivityValidator
{
    private readonly IEnumerable<IPingableAdapter> _adapters;
    private readonly ILogger<AdapterConnectivityValidator> _logger;

    public AdapterConnectivityValidator(
        IEnumerable<IPingableAdapter> adapters,
        ILogger<AdapterConnectivityValidator> logger)
    {
        _adapters = adapters ?? throw new ArgumentNullException(nameof(adapters));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Pings all registered adapters concurrently and returns results.
    /// </summary>
    public async Task<AdapterConnectivityReport> ValidateAllAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var adapterList = _adapters.ToList();

        _logger.LogInformation(
            "[ConnectivityValidator] Starting ping for {Count} adapters...",
            adapterList.Count);

        var tasks = adapterList.Select(async adapter =>
        {
            var itemSw = Stopwatch.StartNew();
            try
            {
                var reachable = await adapter.PingAsync(ct).ConfigureAwait(false);
                itemSw.Stop();
                return new AdapterPingResult(
                    adapter.PlatformCode,
                    reachable,
                    itemSw.Elapsed,
                    null);
            }
            catch (OperationCanceledException)
            {
                itemSw.Stop();
                return new AdapterPingResult(
                    adapter.PlatformCode,
                    false,
                    itemSw.Elapsed,
                    "Cancelled");
            }
            catch (Exception ex)
            {
                itemSw.Stop();
                _logger.LogWarning(ex,
                    "[ConnectivityValidator] {Platform} ping exception",
                    adapter.PlatformCode);
                return new AdapterPingResult(
                    adapter.PlatformCode,
                    false,
                    itemSw.Elapsed,
                    ex.Message);
            }
        });

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        sw.Stop();

        var report = new AdapterConnectivityReport(
            results.ToList().AsReadOnly(),
            sw.Elapsed);

        _logger.LogInformation(
            "[ConnectivityValidator] Complete: {Reachable}/{Total} reachable in {Elapsed}ms",
            report.ReachableCount, report.TotalCount, sw.ElapsedMilliseconds);

        return report;
    }
}

/// <summary>Single adapter ping result.</summary>
public sealed record AdapterPingResult(
    string PlatformCode,
    bool IsReachable,
    TimeSpan ResponseTime,
    string? Error);

/// <summary>Consolidated connectivity report for all adapters.</summary>
public sealed record AdapterConnectivityReport(
    IReadOnlyList<AdapterPingResult> Results,
    TimeSpan TotalElapsed)
{
    public int TotalCount => Results.Count;
    public int ReachableCount => Results.Count(r => r.IsReachable);
    public int UnreachableCount => Results.Count(r => !r.IsReachable);
    public bool AllReachable => UnreachableCount == 0;
}
