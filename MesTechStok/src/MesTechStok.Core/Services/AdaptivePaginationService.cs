using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MesTechStok.Core.Services
{
    /// <summary>
    /// Adaptive pagination sonuç modeli
    /// </summary>
    public class PaginatedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => PageNumber < TotalPages;
        public bool HasPreviousPage => PageNumber > 1;
        public string? NextPageToken { get; set; }
        public string? PreviousPageToken { get; set; }
        public TimeSpan QueryDuration { get; set; }
        public int OptimalPageSize { get; set; }
        public string Strategy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Adaptive pagination ayarları
    /// </summary>
    public class AdaptivePaginationSettings
    {
        public int MinPageSize { get; set; } = 10;
        public int MaxPageSize { get; set; } = 1000;
        public int DefaultPageSize { get; set; } = 50;
        public TimeSpan TargetResponseTime { get; set; } = TimeSpan.FromMilliseconds(500);
        public TimeSpan MaxAcceptableTime { get; set; } = TimeSpan.FromSeconds(5);
        public double PerformanceThreshold { get; set; } = 0.8; // 80% performance threshold
        public bool EnableCaching { get; set; } = true;
        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Performance metrik bilgileri
    /// </summary>
    public class PaginationMetrics
    {
        public string QueryType { get; set; } = string.Empty;
        public int PageSize { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public int ResultCount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public double PerformanceScore { get; set; }
        public string Strategy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Adaptive pagination interface
    /// Performans metriklerine göre otomatik sayfa boyutu optimizasyonu yapar
    /// </summary>
    public interface IAdaptivePaginationService
    {
        Task<PaginatedResult<T>> PaginateAsync<T>(
            IQueryable<T> query,
            int pageNumber,
            int? requestedPageSize = null,
            string? queryType = null);

        Task<PaginatedResult<T>> PaginateWithTokenAsync<T>(
            Func<string?, int, Task<(IEnumerable<T> Items, string? NextToken)>> queryFunc,
            string? pageToken = null,
            int? requestedPageSize = null,
            string? queryType = null);

        int GetOptimalPageSize(string queryType, int? requestedSize = null);
        Task<IEnumerable<PaginationMetrics>> GetMetricsAsync(string? queryType = null, int take = 100);
        Task ClearMetricsAsync(string? queryType = null);
    }

    /// <summary>
    /// Adaptive pagination service implementasyonu
    /// Performans bazlı otomatik sayfa boyutu optimizasyonu ve çeşitli pagination stratejileri
    /// </summary>
    public class AdaptivePaginationService : IAdaptivePaginationService
    {
        private readonly ILogger<AdaptivePaginationService> _logger;
        private readonly AdaptivePaginationSettings _settings;

        // In-memory performance cache (production'da Redis kullanılabilir)
        private readonly Dictionary<string, List<PaginationMetrics>> _performanceCache = new();
        private readonly Dictionary<string, int> _optimalPageSizes = new();
        private readonly object _cacheLock = new object();

        public AdaptivePaginationService(
            ILogger<AdaptivePaginationService> logger,
            AdaptivePaginationSettings? settings = null)
        {
            _logger = logger;
            _settings = settings ?? new AdaptivePaginationSettings();
        }

        /// <summary>
        /// IQueryable için adaptive pagination
        /// </summary>
        public async Task<PaginatedResult<T>> PaginateAsync<T>(
            IQueryable<T> query,
            int pageNumber,
            int? requestedPageSize = null,
            string? queryType = null)
        {
            var startTime = DateTime.UtcNow;
            queryType ??= typeof(T).Name;

            // Optimal sayfa boyutunu hesapla
            var optimalPageSize = GetOptimalPageSize(queryType, requestedPageSize);
            var actualPageSize = requestedPageSize ?? optimalPageSize;

            // Sayfa boyutu sınırlarını kontrol et
            actualPageSize = Math.Max(_settings.MinPageSize, Math.Min(_settings.MaxPageSize, actualPageSize));

            _logger.LogDebug("[AdaptivePagination] {QueryType} - Page {Page}, Size {Size} (optimal: {Optimal})",
                queryType, pageNumber, actualPageSize, optimalPageSize);

            try
            {
                // Total count (cached veya efficient count stratejisi)
                var totalCount = await GetTotalCountAsync(query, queryType);

                // Sayfa verilerini getir
                var skip = (pageNumber - 1) * actualPageSize;
                var items = query.Skip(skip).Take(actualPageSize);
                var results = items.ToList(); // Execute query

                var duration = DateTime.UtcNow - startTime;

                // Performance metriğini kaydet
                await RecordMetricsAsync(queryType, actualPageSize, duration, results.Count, "IQueryable");

                // Sonuçları döndür
                var result = new PaginatedResult<T>
                {
                    Items = results,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = actualPageSize,
                    QueryDuration = duration,
                    OptimalPageSize = optimalPageSize,
                    Strategy = "IQueryable"
                };

                _logger.LogDebug("[AdaptivePagination] {QueryType} completed - {Count} items in {Duration}ms",
                    queryType, results.Count, duration.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "[AdaptivePagination] {QueryType} failed after {Duration}ms",
                    queryType, duration.TotalMilliseconds);

                // Hata durumunda boş sonuç döndür
                return new PaginatedResult<T>
                {
                    Items = Enumerable.Empty<T>(),
                    PageNumber = pageNumber,
                    PageSize = actualPageSize,
                    QueryDuration = duration,
                    Strategy = "Error"
                };
            }
        }

        /// <summary>
        /// Token-based pagination (cursor-based)
        /// </summary>
        public async Task<PaginatedResult<T>> PaginateWithTokenAsync<T>(
            Func<string?, int, Task<(IEnumerable<T> Items, string? NextToken)>> queryFunc,
            string? pageToken = null,
            int? requestedPageSize = null,
            string? queryType = null)
        {
            var startTime = DateTime.UtcNow;
            queryType ??= $"Token_{typeof(T).Name}";

            var optimalPageSize = GetOptimalPageSize(queryType, requestedPageSize);
            var actualPageSize = requestedPageSize ?? optimalPageSize;
            actualPageSize = Math.Max(_settings.MinPageSize, Math.Min(_settings.MaxPageSize, actualPageSize));

            try
            {
                var (items, nextToken) = await queryFunc(pageToken, actualPageSize);
                var results = items.ToList();
                var duration = DateTime.UtcNow - startTime;

                // Performance metriğini kaydet
                await RecordMetricsAsync(queryType, actualPageSize, duration, results.Count, "Token");

                var result = new PaginatedResult<T>
                {
                    Items = results,
                    TotalCount = -1, // Token-based pagination'da total count bilinmez
                    PageNumber = -1,
                    PageSize = actualPageSize,
                    NextPageToken = nextToken,
                    PreviousPageToken = pageToken,
                    QueryDuration = duration,
                    OptimalPageSize = optimalPageSize,
                    Strategy = "Token"
                };

                _logger.LogDebug("[AdaptivePagination] {QueryType} token-based completed - {Count} items in {Duration}ms",
                    queryType, results.Count, duration.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "[AdaptivePagination] {QueryType} token-based failed after {Duration}ms",
                    queryType, duration.TotalMilliseconds);

                return new PaginatedResult<T>
                {
                    Items = Enumerable.Empty<T>(),
                    PageSize = actualPageSize,
                    QueryDuration = duration,
                    Strategy = "TokenError"
                };
            }
        }

        /// <summary>
        /// Query type için optimal sayfa boyutunu hesaplar
        /// </summary>
        public int GetOptimalPageSize(string queryType, int? requestedSize = null)
        {
            if (requestedSize.HasValue)
            {
                // Kullanıcı talepli boyut varsa, sınırlar içinde döndür
                return Math.Max(_settings.MinPageSize, Math.Min(_settings.MaxPageSize, requestedSize.Value));
            }

            lock (_cacheLock)
            {
                if (_optimalPageSizes.TryGetValue(queryType, out var cachedOptimal))
                {
                    return cachedOptimal;
                }

                // Performance metrics'e göre optimal boyutu hesapla
                if (_performanceCache.TryGetValue(queryType, out var metrics) && metrics.Any())
                {
                    var recentMetrics = metrics
                        .Where(m => m.Timestamp > DateTime.UtcNow.AddMinutes(-30)) // Son 30 dakika
                        .ToList();

                    if (recentMetrics.Any())
                    {
                        // En iyi performans gösteren sayfa boyutunu bul
                        var bestMetric = recentMetrics
                            .Where(m => m.ResponseTime <= _settings.TargetResponseTime)
                            .OrderByDescending(m => m.PerformanceScore)
                            .ThenByDescending(m => m.PageSize)
                            .FirstOrDefault();

                        if (bestMetric != null)
                        {
                            _optimalPageSizes[queryType] = bestMetric.PageSize;
                            return bestMetric.PageSize;
                        }

                        // Target time içinde sonuç yoksa, en hızlı olanı al
                        var fastestMetric = recentMetrics
                            .OrderBy(m => m.ResponseTime)
                            .First();

                        _optimalPageSizes[queryType] = fastestMetric.PageSize;
                        return fastestMetric.PageSize;
                    }
                }

                // Default boyut
                return _settings.DefaultPageSize;
            }
        }

        /// <summary>
        /// Performance metriklerini getirir
        /// </summary>
        public async Task<IEnumerable<PaginationMetrics>> GetMetricsAsync(string? queryType = null, int take = 100)
        {
            await Task.CompletedTask;

            lock (_cacheLock)
            {
                if (string.IsNullOrEmpty(queryType))
                {
                    return _performanceCache.Values
                        .SelectMany(x => x)
                        .OrderByDescending(x => x.Timestamp)
                        .Take(take)
                        .ToList();
                }

                if (_performanceCache.TryGetValue(queryType, out var metrics))
                {
                    return metrics
                        .OrderByDescending(x => x.Timestamp)
                        .Take(take)
                        .ToList();
                }

                return Enumerable.Empty<PaginationMetrics>();
            }
        }

        /// <summary>
        /// Performance metriklerini temizler
        /// </summary>
        public async Task ClearMetricsAsync(string? queryType = null)
        {
            await Task.CompletedTask;

            lock (_cacheLock)
            {
                if (string.IsNullOrEmpty(queryType))
                {
                    _performanceCache.Clear();
                    _optimalPageSizes.Clear();
                }
                else
                {
                    _performanceCache.Remove(queryType);
                    _optimalPageSizes.Remove(queryType);
                }
            }

            _logger.LogInformation("[AdaptivePagination] Cleared metrics for {QueryType}",
                queryType ?? "all query types");
        }

        #region Private Helper Methods

        /// <summary>
        /// Total count'u efficient bir şekilde hesaplar
        /// </summary>
        private async Task<int> GetTotalCountAsync<T>(IQueryable<T> query, string queryType)
        {
            // TODO: Caching mechanism eklenebilir
            // TODO: Approximate count stratejileri eklenebilir
            await Task.CompletedTask;
            return query.Count();
        }

        /// <summary>
        /// Performance metriğini kaydeder
        /// </summary>
        private async Task RecordMetricsAsync(string queryType, int pageSize, TimeSpan responseTime, int resultCount, string strategy)
        {
            await Task.CompletedTask;

            var performanceScore = CalculatePerformanceScore(pageSize, responseTime, resultCount);

            var metric = new PaginationMetrics
            {
                QueryType = queryType,
                PageSize = pageSize,
                ResponseTime = responseTime,
                ResultCount = resultCount,
                PerformanceScore = performanceScore,
                Strategy = strategy
            };

            lock (_cacheLock)
            {
                if (!_performanceCache.ContainsKey(queryType))
                {
                    _performanceCache[queryType] = new List<PaginationMetrics>();
                }

                _performanceCache[queryType].Add(metric);

                // Son 1000 metrik tut (memory management)
                if (_performanceCache[queryType].Count > 1000)
                {
                    _performanceCache[queryType] = _performanceCache[queryType]
                        .OrderByDescending(x => x.Timestamp)
                        .Take(500)
                        .ToList();
                }

                // Optimal page size cache'ini invalidate et
                _optimalPageSizes.Remove(queryType);
            }

            _logger.LogTrace("[AdaptivePagination] Recorded metric: {QueryType} - Size {Size}, Time {Time}ms, Score {Score}",
                queryType, pageSize, responseTime.TotalMilliseconds, performanceScore);
        }

        /// <summary>
        /// Performance skorunu hesaplar
        /// </summary>
        private double CalculatePerformanceScore(int pageSize, TimeSpan responseTime, int resultCount)
        {
            // Temel skor: sayfa boyutu / response time oranı
            var baseScore = (double)pageSize / Math.Max(responseTime.TotalMilliseconds, 1);

            // Target response time bonus/penalty
            var timeRatio = _settings.TargetResponseTime.TotalMilliseconds / Math.Max(responseTime.TotalMilliseconds, 1);
            var timeFactor = Math.Min(timeRatio, 2.0); // Maximum 2x bonus

            // Result efficiency bonus
            var resultEfficiency = resultCount > 0 ? (double)resultCount / pageSize : 0.1;

            return baseScore * timeFactor * resultEfficiency;
        }

        #endregion
    }
}
