using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Caching;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Gunde 1 kez Trendyol kategori agacini ceker ve cache'e yazar.
/// </summary>
[AutomaticRetry(Attempts = 3)]
public class CategorySyncJob : ISyncJob
{
    public string JobId => "category-sync";
    public string CronExpression => "0 4 * * *"; // Her gun 04:00

    private readonly TrendyolAdapter _trendyolAdapter;
    private readonly ICacheService _cache;
    private readonly ILogger<CategorySyncJob> _logger;

    public CategorySyncJob(TrendyolAdapter trendyolAdapter, ICacheService cache, ILogger<CategorySyncJob> logger)
    {
        _trendyolAdapter = trendyolAdapter;
        _cache = cache;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Kategori sync basliyor...", JobId);

        try
        {
            var categories = await _trendyolAdapter.GetCategoriesAsync(ct).ConfigureAwait(false);

            await _cache.SetAsync(
                CacheKeys.Categories("trendyol"),
                categories,
                CacheKeys.CategoryTTL,
                ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[{JobId}] Kategori sync tamamlandi: {Count} kategori cache'e yazildi (TTL: 24h)",
                JobId, categories.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobId}] Kategori sync HATA", JobId);
            throw;
        }
    }
}
