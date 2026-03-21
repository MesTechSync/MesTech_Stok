using MesTech.Application.Interfaces.Erp;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// ERP hesap sync job — her gece 03:00'te calisir.
/// MesTech musterilerini ERP'ye olusturur, ERP bakiye bilgilerini gunceller.
/// Simdilk log-only placeholder — real structure.
/// </summary>
[AutomaticRetry(Attempts = 3)]
public class ErpAccountSyncJob : ISyncJob
{
    public string JobId => "erp-account-sync";
    public string CronExpression => "0 3 * * *"; // Her gece 03:00

    private readonly IErpAdapterFactory _factory;
    private readonly ILogger<ErpAccountSyncJob> _logger;

    public ErpAccountSyncJob(IErpAdapterFactory factory, ILogger<ErpAccountSyncJob> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
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

#pragma warning disable CA1031 // Intentional: per-adapter isolation — one failure must not stop others
            try
            {
                var adapter = _factory.GetAdapter(provider);

                // Step 1: Get ERP account balances
                _logger.LogInformation(
                    "[{JobId}] Fetching account balances from {Provider}...", JobId, provider);

                var accounts = await adapter.GetAccountBalancesAsync(ct);

                if (accounts.Count == 0)
                {
                    _logger.LogInformation(
                        "[{JobId}] {Provider} returned 0 accounts", JobId, provider);
                    continue;
                }

                _logger.LogInformation(
                    "[{JobId}] {Provider} returned {Count} accounts — total balance: {TotalBalance:N2}",
                    JobId, provider, accounts.Count,
                    accounts.Sum(a => a.Balance));

                // [Phase-2]: Sync new MesTech customers to ERP + update local balance records

                totalAccounts += accounts.Count;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[{JobId}] ERP hesap sync iptal edildi ({Provider})", JobId, provider);
                throw;
            }
            catch (Exception ex)
            {
                totalFailed++;
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
