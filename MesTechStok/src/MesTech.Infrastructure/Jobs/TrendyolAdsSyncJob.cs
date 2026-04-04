using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Gunluk Trendyol reklam kampanya performansini ceker.
/// Cevrimi: Pull campaigns → her biri icin son 1 gun performance → logla.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class TrendyolAdsSyncJob : ISyncJob
{
    public string JobId => "trendyol-ads-sync";
    public string CronExpression => "0 7 * * *"; // Her gun 07:00

    private readonly IAdapterFactory _factory;
    private readonly ILogger<TrendyolAdsSyncJob> _logger;

    public TrendyolAdsSyncJob(IAdapterFactory factory, ILogger<TrendyolAdsSyncJob> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Trendyol ads sync basliyor...", JobId);

        try
        {
            var adapter = _factory.Resolve("Trendyol") as TrendyolAdapter;
            if (adapter == null)
            {
                _logger.LogWarning("[{JobId}] TrendyolAdapter bulunamadi, atlaniyor", JobId);
                return;
            }

            var campaigns = await adapter.GetAdCampaignsAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("[{JobId}] {Count} aktif kampanya bulundu", JobId, campaigns.Count);

            var yesterday = DateTime.UtcNow.Date.AddDays(-1);
            var today = DateTime.UtcNow.Date;
            int totalMetrics = 0;

            foreach (var campaign in campaigns)
            {
                if (ct.IsCancellationRequested) break;

                var metrics = await adapter.GetAdPerformanceAsync(campaign.CampaignId, yesterday, today, ct)
                    .ConfigureAwait(false);

                totalMetrics += metrics.Count;

                if (metrics.Count > 0)
                {
                    var totalSpend = metrics.Sum(m => m.Spend);
                    var totalRevenue = metrics.Sum(m => m.Revenue);
                    var totalClicks = metrics.Sum(m => m.Clicks);

                    _logger.LogInformation(
                        "[{JobId}] Kampanya '{Name}' (ID={CampaignId}): Harcama={Spend:C}, Gelir={Revenue:C}, Tiklama={Clicks}",
                        JobId, campaign.Name, campaign.CampaignId, totalSpend, totalRevenue, totalClicks);
                }
            }

            _logger.LogInformation(
                "[{JobId}] Trendyol ads sync tamamlandi: {Campaigns} kampanya, {Metrics} metrik satiri",
                JobId, campaigns.Count, totalMetrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobId}] Trendyol ads sync HATA", JobId);
            throw;
        }
    }
}
