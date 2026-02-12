using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
// BackgroundService bağımlılığını kaldırıyoruz; timer tabanlı servis
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;
using Newtonsoft.Json;

namespace MesTechStok.Core.Services
{
    /// <summary>
    /// Sync işlemlerinde başarısız olan item'ları retry eden background service.
    /// Exponential backoff ile belirli aralıklarla tekrar dener.
    /// </summary>
    public interface ISyncRetryService
    {
        Task AddRetryItemAsync<T>(string syncType, string itemId, T itemData, string error, string errorCategory);
        Task<IEnumerable<SyncRetryItem>> GetPendingRetriesAsync(string? syncType = null);
        Task ProcessPendingRetriesAsync(CancellationToken cancellationToken = default);
        Task MarkAsResolvedAsync(long retryItemId);
        Task<int> GetPendingRetryCountAsync(string? syncType = null);
    }

    public class SyncRetryService : ISyncRetryService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SyncRetryService> _logger;
        private readonly TimeSpan _processInterval = TimeSpan.FromMinutes(5); // Her 5 dakikada çalış
        private readonly Timer _timer;

        public SyncRetryService(
            IServiceProvider serviceProvider,
            ILogger<SyncRetryService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _timer = new Timer(async _ => await SafeProcessAsync(), null, _processInterval, _processInterval);
        }

        private async Task SafeProcessAsync()
        {
            try { await ProcessPendingRetriesAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Error in SyncRetryService timer loop"); }
        }

        /// <summary>
        /// Başarısız sync item'ını retry listesine ekler
        /// </summary>
        public async Task AddRetryItemAsync<T>(string syncType, string itemId, T itemData, string error, string errorCategory)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var retryItem = new SyncRetryItem
                {
                    SyncType = syncType,
                    ItemId = itemId,
                    ItemType = typeof(T).Name,
                    ItemData = JsonConvert.SerializeObject(itemData),
                    LastError = error,
                    ErrorCategory = errorCategory,
                    CorrelationId = MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId
                };

                retryItem.CalculateNextRetry();

                context.SyncRetryItems.Add(retryItem);
                await context.SaveChangesAsync();

                _logger.LogWarning("Added retry item: {SyncType} {ItemId} - {Error}",
                    syncType, itemId, error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add retry item: {SyncType} {ItemId}", syncType, itemId);
            }
        }

        /// <summary>
        /// Bekleyen retry item'larını getirir
        /// </summary>
        public async Task<IEnumerable<SyncRetryItem>> GetPendingRetriesAsync(string? syncType = null)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var query = context.SyncRetryItems
                .Where(r => !r.IsResolved)
                .Where(r => r.NextRetryUtc != null && r.NextRetryUtc <= DateTime.UtcNow)
                .Where(r => r.RetryCount < r.MaxRetries);

            if (!string.IsNullOrEmpty(syncType))
            {
                query = query.Where(r => r.SyncType == syncType);
            }

            return await query
                .OrderBy(r => r.NextRetryUtc)
                .Take(100) // Maximum 100 item tek seferde
                .ToListAsync();
        }

        /// <summary>
        /// Bekleyen retry sayısını getirir
        /// </summary>
        public async Task<int> GetPendingRetryCountAsync(string? syncType = null)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var query = context.SyncRetryItems
                .Where(r => !r.IsResolved)
                .Where(r => r.RetryCount < r.MaxRetries);

            if (!string.IsNullOrEmpty(syncType))
            {
                query = query.Where(r => r.SyncType == syncType);
            }

            return await query.CountAsync();
        }

        /// <summary>
        /// Retry item'ını çözümlendi olarak işaretler
        /// </summary>
        public async Task MarkAsResolvedAsync(long retryItemId)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var item = await context.SyncRetryItems.FindAsync(retryItemId);
                if (item != null)
                {
                    item.MarkAsResolved();
                    await context.SaveChangesAsync();

                    _logger.LogInformation("Marked retry item as resolved: {ItemId}", retryItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark retry item as resolved: {ItemId}", retryItemId);
            }
        }

        /// <summary>
        /// Bekleyen retry'ları işler
        /// </summary>
        public async Task ProcessPendingRetriesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var pendingRetries = await GetPendingRetriesAsync();
                var retryItems = pendingRetries.ToList();

                if (!retryItems.Any())
                {
                    return;
                }

                _logger.LogInformation("Processing {Count} pending retry items", retryItems.Count);

                var groupedRetries = retryItems.GroupBy(r => r.SyncType);

                foreach (var group in groupedRetries)
                {
                    await ProcessRetriesForSyncType(group.Key, group.ToList(), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending retries");
            }
        }

        /// <summary>
        /// Belirli sync type için retry'ları işler
        /// </summary>
        private async Task ProcessRetriesForSyncType(string syncType, List<SyncRetryItem> retryItems, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var retryItem in retryItems)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();

                    var success = await ProcessSingleRetryItem(retryItem, scope.ServiceProvider);

                    if (success)
                    {
                        retryItem.MarkAsResolved();
                        _logger.LogInformation("Retry successful: {SyncType} {ItemId}",
                            retryItem.SyncType, retryItem.ItemId);
                    }
                    else
                    {
                        retryItem.IncrementRetry($"Retry #{retryItem.RetryCount + 1} failed", retryItem.ErrorCategory);
                        _logger.LogWarning("Retry failed: {SyncType} {ItemId} (attempt {RetryCount}/{MaxRetries})",
                            retryItem.SyncType, retryItem.ItemId, retryItem.RetryCount, retryItem.MaxRetries);
                    }

                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing retry item: {SyncType} {ItemId}",
                        retryItem.SyncType, retryItem.ItemId);

                    retryItem.IncrementRetry($"Exception: {ex.Message}", "Exception");
                    await context.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// Tek bir retry item'ını işler
        /// </summary>
        private async Task<bool> ProcessSingleRetryItem(SyncRetryItem retryItem, IServiceProvider serviceProvider)
        {
            try
            {
                switch (retryItem.SyncType)
                {
                    case "ProductSync":
                        return await ProcessProductRetry(retryItem, serviceProvider);
                    case "OrderSync":
                        return await ProcessOrderRetry(retryItem, serviceProvider);
                    case "StockSync":
                        return await ProcessStockRetry(retryItem, serviceProvider);
                    default:
                        _logger.LogWarning("Unknown sync type: {SyncType}", retryItem.SyncType);
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessSingleRetryItem");
                return false;
            }
        }

        private async Task<bool> ProcessProductRetry(SyncRetryItem retryItem, IServiceProvider serviceProvider)
        {
            // TODO: Product retry logic implementasyonu
            await Task.Delay(100); // Placeholder
            return false;
        }

        private async Task<bool> ProcessOrderRetry(SyncRetryItem retryItem, IServiceProvider serviceProvider)
        {
            // TODO: Order retry logic implementasyonu
            await Task.Delay(100); // Placeholder
            return false;
        }

        private async Task<bool> ProcessStockRetry(SyncRetryItem retryItem, IServiceProvider serviceProvider)
        {
            // TODO: Stock retry logic implementasyonu
            await Task.Delay(100); // Placeholder
            return false;
        }

        public void Dispose()
        {
            try { _timer?.Dispose(); } catch { }
        }
    }
}
