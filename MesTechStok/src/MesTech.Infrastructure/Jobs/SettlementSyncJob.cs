using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Gunde 1 kez Trendyol cari hesap ekstresini ceker.
/// </summary>
public class SettlementSyncJob : ISyncJob
{
    public string JobId => "settlement-sync";
    public string CronExpression => "0 3 * * *"; // Her gun 03:00

    private readonly ILogger<SettlementSyncJob> _logger;

    public SettlementSyncJob(ILogger<SettlementSyncJob> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Settlement sync basliyor...", JobId);

        // TODO: TrendyolAdapter.GetSettlementAsync() + GetCargoInvoicesAsync()

        await Task.CompletedTask;
        _logger.LogInformation("[{JobId}] Settlement sync tamamlandi", JobId);
    }
}
