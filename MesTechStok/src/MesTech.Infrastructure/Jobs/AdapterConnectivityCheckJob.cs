using MesTech.Infrastructure.Integration.Orchestration;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her saat başı tüm marketplace adapter'ların connectivity'sini doğrular.
/// Unreachable adapter varsa warning log atar — monitoring stack (Seq/Grafana) tarafından yakalanır.
/// </summary>
[AutomaticRetry(Attempts = 1)]
[DisableConcurrentExecution(timeoutInSeconds: 120)]
public sealed class AdapterConnectivityCheckJob : ISyncJob
{
    public string JobId => "adapter-connectivity-check";
    public string CronExpression => "0 * * * *"; // Her saat başı

    private readonly AdapterConnectivityValidator _validator;
    private readonly ILogger<AdapterConnectivityCheckJob> _logger;

    public AdapterConnectivityCheckJob(
        AdapterConnectivityValidator validator,
        ILogger<AdapterConnectivityCheckJob> logger)
    {
        _validator = validator;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Adapter connectivity check başlıyor...", JobId);

        var report = await _validator.ValidateAllAsync(ct).ConfigureAwait(false);

        if (report.AllReachable)
        {
            _logger.LogInformation(
                "[{JobId}] Tüm adapter'lar erişilebilir: {Count}/{Count} ({Elapsed}ms)",
                JobId, report.ReachableCount, report.TotalCount, report.TotalElapsed.TotalMilliseconds);
        }
        else
        {
            foreach (var result in report.Results.Where(r => !r.IsReachable))
            {
                _logger.LogWarning(
                    "[{JobId}] UNREACHABLE: {Platform} — {Error} ({ResponseTime}ms)",
                    JobId, result.PlatformCode, result.Error ?? "timeout", result.ResponseTime.TotalMilliseconds);
            }

            _logger.LogWarning(
                "[{JobId}] Connectivity check: {Reachable}/{Total} reachable, {Unreachable} UNREACHABLE",
                JobId, report.ReachableCount, report.TotalCount, report.UnreachableCount);
        }
    }
}
