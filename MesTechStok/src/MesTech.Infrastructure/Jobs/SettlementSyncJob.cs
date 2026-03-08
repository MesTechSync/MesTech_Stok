using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Gunde 1 kez Trendyol cari hesap ekstresini ceker.
/// </summary>
public class SettlementSyncJob : ISyncJob
{
    public string JobId => "settlement-sync";
    public string CronExpression => "0 3 * * *"; // Her gun 03:00

    private readonly IAdapterFactory _factory;
    private readonly ILogger<SettlementSyncJob> _logger;

    public SettlementSyncJob(IAdapterFactory factory, ILogger<SettlementSyncJob> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Settlement sync basliyor...", JobId);

        try
        {
            var adapter = _factory.ResolveCapability<ISettlementCapableAdapter>("Trendyol");
            if (adapter == null)
            {
                _logger.LogWarning("[{JobId}] Trendyol ISettlementCapableAdapter bulunamadi, atlaniyor", JobId);
                return;
            }

            var yesterday = DateTime.UtcNow.Date.AddDays(-1);
            var today = DateTime.UtcNow.Date;

            var settlement = await adapter.GetSettlementAsync(yesterday, today, ct);
            if (settlement != null)
            {
                _logger.LogInformation(
                    "[{JobId}] Settlement cekildi: {StartDate:d} - {EndDate:d}",
                    JobId, yesterday, today);
            }

            var cargoInvoices = await adapter.GetCargoInvoicesAsync(yesterday, ct);
            _logger.LogInformation(
                "[{JobId}] Settlement sync tamamlandi: {CargoCount} kargo faturasi cekildi",
                JobId, cargoInvoices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobId}] Settlement sync HATA", JobId);
            throw;
        }
    }
}
