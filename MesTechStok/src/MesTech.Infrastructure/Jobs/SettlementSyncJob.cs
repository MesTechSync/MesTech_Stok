using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// [DEPRECATED] Gunde 1 kez TÜM platformların cari hesap ekstresini çeker
/// ama DB'ye KAYDETMEZ — sadece connectivity check olarak çalışır.
/// Gerçek settlement persist işlemi SettlementSyncWorker (accounting-settlement-sync, 03:30) tarafından yapılır.
/// Bu job kaldırılabilir — SettlementSyncWorker yeterlidir.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 600)]
public sealed class SettlementSyncJob : ISyncJob
{
    public string JobId => "settlement-sync";
    public string CronExpression => "0 3 * * *"; // Her gun 03:00

    private static readonly TimeSpan PerPlatformTimeout = TimeSpan.FromSeconds(60);

    private readonly IAdapterFactory _factory;
    private readonly ILogger<SettlementSyncJob> _logger;

    public SettlementSyncJob(IAdapterFactory factory, ILogger<SettlementSyncJob> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Settlement sync basliyor — tüm platformlar...", JobId);

        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var today = DateTime.UtcNow.Date;

        var adapters = _factory.GetAll()
            .OfType<ISettlementCapableAdapter>()
            .ToList();

        if (adapters.Count == 0)
        {
            _logger.LogWarning("[{JobId}] ISettlementCapableAdapter bulunamadi", JobId);
            return;
        }

        _logger.LogInformation("[{JobId}] {Count} platform settlement sync edilecek", JobId, adapters.Count);

        var tasks = adapters.Select(adapter => SyncSinglePlatformAsync(adapter, yesterday, today, ct));
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        var success = results.Count(r => r);
        _logger.LogInformation(
            "[{JobId}] Settlement sync tamamlandi: {Success}/{Total} platform basarili",
            JobId, success, adapters.Count);
    }

    private async Task<bool> SyncSinglePlatformAsync(
        ISettlementCapableAdapter adapter, DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        var platformCode = (adapter as IIntegratorAdapter)?.PlatformCode ?? "unknown";
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(PerPlatformTimeout);

            var settlement = await adapter.GetSettlementAsync(startDate, endDate, cts.Token).ConfigureAwait(false);
            if (settlement != null)
            {
                _logger.LogInformation(
                    "[{JobId}] {Platform} settlement cekildi: {Start:d} - {End:d}",
                    JobId, platformCode, startDate, endDate);
            }

            return true;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("[{JobId}] {Platform} settlement sync TIMEOUT ({Timeout}s)",
                JobId, platformCode, PerPlatformTimeout.TotalSeconds);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobId}] {Platform} settlement sync HATA", JobId, platformCode);
            return false;
        }
    }
}
