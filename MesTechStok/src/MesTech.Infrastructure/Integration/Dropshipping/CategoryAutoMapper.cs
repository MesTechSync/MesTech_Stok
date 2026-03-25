using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Dropshipping;

/// <summary>
/// Tedarikçi kategori yolunu MesTech kategori ID'sine fuzzy-match ile eşleştirir.
/// Levenshtein mesafesi + keyword overlap kombinasyonu, IMemoryCache ile 24h cache.
/// Manuel mapping override destekler (IMemoryCache kalıcı değil — yeniden başlatmada sıfırlanır).
/// ENT-DROP-IMP-SPRINT-D — DEV 1 Task D-02
/// </summary>
public sealed class CategoryAutoMapper(
    ICategoryRepository categoryRepo,
    IMemoryCache cache,
    ILogger<CategoryAutoMapper> logger
) : ICategoryMapperService
{
    // DEMİR KARAR: Otomatik eşleşme için minimum güven skoru
    private const decimal AutoMatchThreshold = 0.75m;
    private const string CacheKeyPrefix = "category:map:";
    private const string ManualCacheKeyPrefix = "category:manual:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    public async Task<CategoryMapResult?> MapAsync(
        string supplierCategoryPath,
        string? platformCode = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(supplierCategoryPath))
            return null;

        var normalizedInput = NormalizePath(supplierCategoryPath);
        var cacheKey = $"{CacheKeyPrefix}{normalizedInput.GetHashCode()}";

        // 1. Cache kontrolü
        if (cache.TryGetValue(cacheKey, out CategoryMapResult? cached) && cached is not null)
            return cached;

        // 2. Manuel mapping kontrolü (kullanıcı override)
        var manualKey = $"{ManualCacheKeyPrefix}{normalizedInput.GetHashCode()}";
        if (cache.TryGetValue(manualKey, out Guid manualTargetId) && manualTargetId != Guid.Empty)
        {
            var allCats = await categoryRepo.GetActiveAsync().ConfigureAwait(false);
            var target = allCats.FirstOrDefault(c => c.Id == manualTargetId);
            if (target is not null)
            {
                var manualResult = new CategoryMapResult(
                    target.Id,
                    target.Name,
                    1.0m,
                    IsManual: true,
                    IsExact: true);
                cache.Set(cacheKey, manualResult, CacheTtl);
                return manualResult;
            }
        }

        // 3. Tüm aktif kategorileri al
        var categories = await categoryRepo.GetActiveAsync().ConfigureAwait(false);

        // 4. Kesin eşleşme (normalize ad veya kod)
        var exact = categories.FirstOrDefault(c =>
            NormalizePath(c.Name) == normalizedInput ||
            NormalizePath(c.Code) == normalizedInput);

        if (exact is not null)
        {
            var exactResult = new CategoryMapResult(
                exact.Id, exact.Name,
                1.0m, IsManual: false, IsExact: true);
            cache.Set(cacheKey, exactResult, CacheTtl);
            return exactResult;
        }

        // 5. Fuzzy match — leaf category ve tam yol karşılaştır
        var leafCategory = FuzzyMatcher.ExtractLeafCategory(supplierCategoryPath);

        CategoryMapResult? best = null;
        decimal bestScore = 0;

        foreach (var category in categories)
        {
            var leafScore = FuzzyMatcher.CombinedScore(leafCategory, category.Name);
            var fullScore = FuzzyMatcher.CombinedScore(normalizedInput, NormalizePath(category.Name));
            var codeScore = FuzzyMatcher.CombinedScore(normalizedInput, NormalizePath(category.Code));

            var score = Math.Max(Math.Max(leafScore, fullScore), codeScore);

            if (score > bestScore)
            {
                bestScore = score;
                best = new CategoryMapResult(
                    category.Id,
                    category.Name,
                    score,
                    IsManual: false,
                    IsExact: false);
            }
        }

        // 6. Eşik kontrolü
        if (best is null || best.Confidence < AutoMatchThreshold)
        {
            logger.LogDebug(
                "Kategori eşleşmesi bulunamadı: '{Path}' (en yüksek skor: {Score:P0})",
                supplierCategoryPath, bestScore);
            return null;
        }

        logger.LogDebug(
            "Kategori eşleşti: '{Input}' → '{Target}' ({Confidence:P0})",
            supplierCategoryPath, best.CategoryPath, best.Confidence);

        cache.Set(cacheKey, best, CacheTtl);
        return best;
    }

    public Task SaveManualMappingAsync(
        string supplierCategoryPath,
        Guid targetCategoryId,
        CancellationToken ct = default)
    {
        var normalizedInput = NormalizePath(supplierCategoryPath);
        var manualKey = $"{ManualCacheKeyPrefix}{normalizedInput.GetHashCode()}";
        var autoKey   = $"{CacheKeyPrefix}{normalizedInput.GetHashCode()}";

        // Manuel mapping kaydet — auto-map cache'ini temizle
        cache.Set(manualKey, targetCategoryId, CacheTtl);
        cache.Remove(autoKey);

        logger.LogInformation(
            "Manuel kategori mapping kaydedildi: '{Path}' → {TargetId}",
            supplierCategoryPath, targetCategoryId);

        return Task.CompletedTask;
    }

    private static string NormalizePath(string path)
        => path.ToLowerInvariant()
               .Replace(" > ", " ")
               .Replace(" / ", " ")
               .Replace(">", " ")
               .Replace("/", " ")
               .Replace("  ", " ")
               .Trim();
}
