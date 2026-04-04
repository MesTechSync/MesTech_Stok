using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her saat Trendyol urun degerlendirmelerini ceker.
/// Cevrimi: Pull reviews → logla (ileride: DB persist + cevap oneri motoru).
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class TrendyolReviewSyncJob : ISyncJob
{
    public string JobId => "trendyol-review-sync";
    public string CronExpression => "0 * * * *"; // Her saat basi

    private readonly IAdapterFactory _factory;
    private readonly ILogger<TrendyolReviewSyncJob> _logger;

    public TrendyolReviewSyncJob(IAdapterFactory factory, ILogger<TrendyolReviewSyncJob> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Trendyol review sync basliyor...", JobId);

        try
        {
            var adapter = _factory.Resolve("Trendyol") as TrendyolAdapter;
            if (adapter == null)
            {
                _logger.LogWarning("[{JobId}] TrendyolAdapter bulunamadi, atlaniyor", JobId);
                return;
            }

            int page = 0;
            int totalFetched = 0;
            const int pageSize = 50;

            while (!ct.IsCancellationRequested)
            {
                var reviews = await adapter.GetProductReviewsAsync(page, pageSize, ct).ConfigureAwait(false);
                if (reviews.Count == 0) break;

                totalFetched += reviews.Count;
                var unreplied = reviews.Count(r => !r.IsReplied);

                _logger.LogInformation(
                    "[{JobId}] Sayfa {Page}: {Count} review ({Unreplied} cevapsiz)",
                    JobId, page, reviews.Count, unreplied);

                if (reviews.Count < pageSize) break;
                page++;
            }

            _logger.LogInformation(
                "[{JobId}] Trendyol review sync tamamlandi: {Total} review cekildi",
                JobId, totalFetched);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobId}] Trendyol review sync HATA", JobId);
            throw;
        }
    }
}
