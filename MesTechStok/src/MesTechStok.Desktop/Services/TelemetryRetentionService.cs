using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MesTechStok.Core.Data;

namespace MesTechStok.Desktop.Services
{
    /// <summary>
    /// Telemetry verilerinin otomatik temizlenmesi için background service.
    /// ApiCallLogs ve CircuitStateLogs tablolarında eski kayıtları (30+ gün) siler.
    /// Performans: günlük çalışır, batch size ile kontrollü temizlik.
    /// </summary>
    public interface ITelemetryRetentionService
    {
        Task ExecuteCleanupAsync(CancellationToken cancellationToken = default);
    }

    public sealed class TelemetryRetentionService : BackgroundService, ITelemetryRetentionService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TelemetryRetentionService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // Günlük temizlik
        private readonly int _retentionDays = 30; // 30 gün veri saklama
        private readonly int _batchSize = 1000; // Her seferde maksimum silinecek kayıt sayısı

        public TelemetryRetentionService(IServiceProvider serviceProvider, ILogger<TelemetryRetentionService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Telemetry retention service started with {RetentionDays} days retention", _retentionDays);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ExecuteCleanupAsync(stoppingToken);
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancelling
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during telemetry cleanup");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Retry after 1 hour on error
                }
            }
        }

        public async Task ExecuteCleanupAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);

            // ApiCallLogs temizliği
            var apiCallsDeleted = await CleanupApiCallLogsAsync(dbContext, cutoffDate, cancellationToken);

            // CircuitStateLogs temizliği
            var circuitStatesDeleted = await CleanupCircuitStateLogsAsync(dbContext, cutoffDate, cancellationToken);

            if (apiCallsDeleted > 0 || circuitStatesDeleted > 0)
            {
                _logger.LogInformation("Telemetry cleanup completed: {ApiCalls} API calls, {CircuitStates} circuit states deleted",
                    apiCallsDeleted, circuitStatesDeleted);
            }
        }

        private async Task<int> CleanupApiCallLogsAsync(AppDbContext dbContext, DateTime cutoffDate, CancellationToken cancellationToken)
        {
            var totalDeleted = 0;
            bool hasMore;

            do
            {
                var toDelete = await dbContext.ApiCallLogs
                    .Where(log => log.TimestampUtc < cutoffDate)
                    .Take(_batchSize)
                    .ToListAsync(cancellationToken);

                hasMore = toDelete.Count == _batchSize;

                if (toDelete.Count > 0)
                {
                    dbContext.ApiCallLogs.RemoveRange(toDelete);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    totalDeleted += toDelete.Count;

                    _logger.LogDebug("Deleted {Count} API call logs batch", toDelete.Count);
                }
            } while (hasMore && !cancellationToken.IsCancellationRequested);

            return totalDeleted;
        }

        private async Task<int> CleanupCircuitStateLogsAsync(AppDbContext dbContext, DateTime cutoffDate, CancellationToken cancellationToken)
        {
            var totalDeleted = 0;
            bool hasMore;

            do
            {
                var toDelete = await dbContext.CircuitStateLogs
                    .Where(log => log.TransitionTimeUtc < cutoffDate)
                    .Take(_batchSize)
                    .ToListAsync(cancellationToken);

                hasMore = toDelete.Count == _batchSize;

                if (toDelete.Count > 0)
                {
                    dbContext.CircuitStateLogs.RemoveRange(toDelete);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    totalDeleted += toDelete.Count;

                    _logger.LogDebug("Deleted {Count} circuit state logs batch", toDelete.Count);
                }
            } while (hasMore && !cancellationToken.IsCancellationRequested);

            return totalDeleted;
        }
    }
}
