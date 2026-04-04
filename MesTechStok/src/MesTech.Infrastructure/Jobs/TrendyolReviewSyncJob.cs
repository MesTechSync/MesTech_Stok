using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Infrastructure.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her saat Trendyol urun degerlendirmelerini ceker.
/// Cevrimi: Pull reviews → cevapsiz review'lar icin event publish → logla.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class TrendyolReviewSyncJob : ISyncJob
{
    public string JobId => "trendyol-review-sync";
    public string CronExpression => "0 * * * *"; // Her saat basi

    private readonly IAdapterFactory _factory;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<TrendyolReviewSyncJob> _logger;

    public TrendyolReviewSyncJob(
        IAdapterFactory factory,
        IPublishEndpoint publishEndpoint,
        ILogger<TrendyolReviewSyncJob> logger)
    {
        _factory = factory;
        _publishEndpoint = publishEndpoint;
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
            int totalUnreplied = 0;
            const int pageSize = 50;

            while (!ct.IsCancellationRequested)
            {
                var reviews = await adapter.GetProductReviewsAsync(page, pageSize, ct: ct).ConfigureAwait(false);
                if (reviews.Count == 0) break;

                totalFetched += reviews.Count;

                foreach (var review in reviews.Where(r => !r.IsReplied))
                {
                    totalUnreplied++;
                    await _publishEndpoint.Publish(new ProductReviewReceivedIntegrationEvent(
                        ReviewId: review.Id,
                        ProductId: review.ProductId,
                        Rating: review.Rate,
                        Comment: review.Comment,
                        IsReplied: false,
                        PlatformCode: "Trendyol",
                        OccurredAt: DateTime.UtcNow), ct).ConfigureAwait(false);
                }

                _logger.LogInformation(
                    "[{JobId}] Sayfa {Page}: {Count} review ({Unreplied} cevapsiz)",
                    JobId, page, reviews.Count, reviews.Count(r => !r.IsReplied));

                if (reviews.Count < pageSize) break;
                page++;
            }

            _logger.LogInformation(
                "[{JobId}] Trendyol review sync tamamlandi: {Total} review, {Unreplied} cevapsiz event publish edildi",
                JobId, totalFetched, totalUnreplied);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobId}] Trendyol review sync HATA", JobId);
            throw;
        }
    }
}
