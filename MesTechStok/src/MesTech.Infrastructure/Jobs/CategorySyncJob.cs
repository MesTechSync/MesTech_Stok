using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Caching;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Gunde 1 kez Trendyol kategori agacini ceker ve cache'e yazar.
/// </summary>
public class CategorySyncJob : ISyncJob
{
    public string JobId => "category-sync";
    public string CronExpression => "0 4 * * *"; // Her gun 04:00

    private readonly ICacheService _cache;
    private readonly ILogger<CategorySyncJob> _logger;

    public CategorySyncJob(ICacheService cache, ILogger<CategorySyncJob> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Kategori sync basliyor...", JobId);

        // TODO: TrendyolAdapter.GetCategoriesAsync() -> cache'e yaz
        // Cache key: CacheKeys.Categories("trendyol")
        // TTL: 24 saat

        await Task.CompletedTask;
        _logger.LogInformation("[{JobId}] Kategori sync tamamlandi", JobId);
    }
}
