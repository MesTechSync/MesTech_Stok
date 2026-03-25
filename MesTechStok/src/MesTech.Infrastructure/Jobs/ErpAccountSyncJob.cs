using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Constants;
using MesTech.Domain.Entities.Erp;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;
using Hangfire;
using System.Diagnostics;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// ERP hesap sync job — her gece 03:00'te calisir.
/// ERP'den hesap bakiye bilgilerini ceker ve sync log kaydi olusturur.
/// Phase-2 TAM: Bakiye verilerini ceker, metrik loglar, IErpAccountCapable ile hesap arama yapar.
/// </summary>
[AutomaticRetry(Attempts = 3)]
public sealed class ErpAccountSyncJob : ISyncJob
{
    public string JobId => "erp-account-sync";
    public string CronExpression => "0 3 * * *"; // Her gece 03:00

    private readonly IErpAdapterFactory _factory;
    private readonly IErpSyncLogRepository _syncLogRepository;
    private readonly ILogger<ErpAccountSyncJob> _logger;

    public ErpAccountSyncJob(
        IErpAdapterFactory factory,
        IErpSyncLogRepository syncLogRepository,
        ILogger<ErpAccountSyncJob> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _syncLogRepository = syncLogRepository ?? throw new ArgumentNullException(nameof(syncLogRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] ERP hesap sync basliyor...", JobId);

        var providers = _factory.SupportedProviders;
        var totalAccounts = 0;
        var totalFailed = 0;

        foreach (var provider in providers)
        {
            ct.ThrowIfCancellationRequested();

            var sw = Stopwatch.StartNew();
            var syncLog = ErpSyncLog.Create(
                DomainConstants.SystemTenantId, // system-level sync, not tenant-specific
                provider,
                "AccountBalance",
                Guid.NewGuid());

#pragma warning disable CA1031 // Intentional: per-adapter isolation — one failure must not stop others
            try
            {
                var adapter = _factory.GetAdapter(provider);

                _logger.LogInformation(
                    "[{JobId}] Fetching account balances from {Provider}...", JobId, provider);

                var accounts = await adapter.GetAccountBalancesAsync(ct).ConfigureAwait(false);

                if (accounts.Count == 0)
                {
                    _logger.LogInformation(
                        "[{JobId}] {Provider} returned 0 accounts", JobId, provider);

                    sw.Stop();
                    syncLog.MarkSuccess($"{provider}-accounts-0");
                    syncLog.SetDetails(0, 0, 0, 0, sw.ElapsedMilliseconds, triggeredBy: "Hangfire");
                    await _syncLogRepository.AddAsync(syncLog, ct).ConfigureAwait(false);
                    continue;
                }

                var totalBalance = accounts.Sum(a => a.Balance);
                var positiveCount = accounts.Count(a => a.Balance > 0);
                var negativeCount = accounts.Count(a => a.Balance < 0);

                _logger.LogInformation(
                    "[{JobId}] {Provider} returned {Count} accounts — total balance: {TotalBalance:N2} | positive: {Positive} | negative: {Negative}",
                    JobId, provider, accounts.Count, totalBalance, positiveCount, negativeCount);

                // Account capability check — search for account details if supported
                if (adapter is IErpAccountCapable accountCapable)
                {
                    var negativeBalanceAccounts = accounts.Where(a => a.Balance < 0).ToList();
                    foreach (var account in negativeBalanceAccounts.Take(10))
                    {
                        ct.ThrowIfCancellationRequested();

                        var detail = await accountCapable.GetAccountAsync(account.AccountCode, ct).ConfigureAwait(false);
                        if (detail is not null)
                        {
                            _logger.LogWarning(
                                "[{JobId}] {Provider} negative balance: {AccountCode} ({AccountName}) = {Balance:N2}",
                                JobId, provider, detail.AccountCode, detail.AccountName, detail.Balance);
                        }
                    }
                }

                totalAccounts += accounts.Count;

                sw.Stop();
                syncLog.MarkSuccess($"{provider}-accounts-{accounts.Count}");
                syncLog.SetDetails(accounts.Count, accounts.Count, 0, 0, sw.ElapsedMilliseconds, triggeredBy: "Hangfire");
                await _syncLogRepository.AddAsync(syncLog, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[{JobId}] ERP hesap sync iptal edildi ({Provider})", JobId, provider);
                throw;
            }
            catch (Exception ex)
            {
                totalFailed++;
                sw.Stop();
                syncLog.MarkFailure(ex.Message);
                syncLog.SetDetails(0, 0, 1, 0, sw.ElapsedMilliseconds, ex.ToString(), "Hangfire");
                await _syncLogRepository.AddAsync(syncLog, ct).ConfigureAwait(false);

                _logger.LogError(ex,
                    "[{JobId}] {Provider} hesap sync HATA", JobId, provider);
            }
#pragma warning restore CA1031
        }

        _logger.LogInformation(
            "[{JobId}] ERP hesap sync tamamlandi: {Accounts} accounts from {Providers} providers, {Failed} failed",
            JobId, totalAccounts, providers.Count, totalFailed);
    }
}
