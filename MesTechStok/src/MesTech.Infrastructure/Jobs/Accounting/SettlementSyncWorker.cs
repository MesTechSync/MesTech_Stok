using MesTech.Application.Interfaces;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs.Accounting;

/// <summary>
/// Platform settlement verilerini periyodik olarak ceken Hangfire worker.
/// Yapilandirilan platformlardan ISettlementCapableAdapter uzerinden settlement verisini alir.
/// Her gun 03:30'da calisir.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class SettlementSyncWorker : IAccountingJob
{
    public string JobId => "accounting-settlement-sync";
    public string CronExpression => "30 3 * * *"; // Her gun 03:30

    private readonly IAdapterFactory _adapterFactory;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SettlementSyncWorker> _logger;

    // Tüm ISettlementCapableAdapter implement eden platformlar otomatik dahil

    public SettlementSyncWorker(
        IAdapterFactory adapterFactory,
        ITenantProvider tenantProvider,
        ILogger<SettlementSyncWorker> logger)
    {
        _adapterFactory = adapterFactory;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Settlement sync basliyor...", JobId);

        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var today = DateTime.UtcNow.Date;
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var totalSettlements = 0;

        var settlementAdapters = _adapterFactory.GetAll()
            .Where(a => a is ISettlementCapableAdapter)
            .ToList();

        foreach (var registeredAdapter in settlementAdapters)
        {
            ct.ThrowIfCancellationRequested();
            var platform = registeredAdapter.PlatformCode;

            try
            {
                var settlementAdapter = registeredAdapter as ISettlementCapableAdapter;
                if (settlementAdapter == null) continue;

                var settlement = await settlementAdapter.GetSettlementAsync(yesterday, today, ct).ConfigureAwait(false);
                if (settlement != null)
                {
                    totalSettlements++;
                    _logger.LogInformation(
                        "[{JobId}] {Platform} settlement cekildi: {StartDate:d} - {EndDate:d}",
                        JobId, platform, yesterday, today);
                }

                var cargoInvoices = await settlementAdapter.GetCargoInvoicesAsync(yesterday, ct).ConfigureAwait(false);
                _logger.LogInformation(
                    "[{JobId}] {Platform} — {CargoCount} kargo faturasi cekildi",
                    JobId, platform, cargoInvoices.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{JobId}] {Platform} settlement sync HATA", JobId, platform);
                // Devam et — diger platformlari denemeyi birakma
            }
        }

        _logger.LogInformation(
            "[{JobId}] Settlement sync tamamlandi — {Total} platform settlement cekildi",
            JobId, totalSettlements);
    }
}
