using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 30 dakikada OpenCart ile cift yonlu stok sync yapar.
/// </summary>
public class OpenCartStockSyncJob : ISyncJob
{
    public string JobId => "opencart-stock-sync";
    public string CronExpression => "*/30 * * * *"; // Her 30 dk

    private readonly ILogger<OpenCartStockSyncJob> _logger;

    public OpenCartStockSyncJob(ILogger<OpenCartStockSyncJob> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] OpenCart stok sync basliyor...", JobId);

        // TODO: OpenCartAdapter uzerinden cift yonlu stok sync

        await Task.CompletedTask;
        _logger.LogInformation("[{JobId}] OpenCart stok sync tamamlandi", JobId);
    }
}
