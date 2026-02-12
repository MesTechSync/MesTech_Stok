using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;
using MesTechStok.Desktop.Utils;

namespace MesTechStok.Desktop.Services
{
    public interface IOfflineQueueService
    {
        Task<int> EnqueueAsync(string channel, string direction, string payload, string? correlationId = null);
        Task<int> CleanupExpiredAsync();
        Task<OfflineQueueItem?> DequeueNextPendingAsync();
        Task MarkSucceededAsync(int id);
        Task MarkFailedAsync(int id, string errorMessage, int? backoffSeconds = null);
    }

    public class OfflineQueueService : IOfflineQueueService
    {
        private readonly AppDbContext _db;
        private readonly ResilienceOptions _options;

        public OfflineQueueService(AppDbContext db, IOptions<ResilienceOptions> options)
        {
            _db = db;
            _options = options.Value;
        }

        public async Task<int> EnqueueAsync(string channel, string direction, string payload, string? correlationId = null)
        {
            var item = new OfflineQueueItem
            {
                Channel = channel,
                Direction = direction,
                Payload = payload,
                Status = "Pending",
                RetryCount = 0,
                NextAttemptAt = DateTime.UtcNow,
                CorrelationId = correlationId
            };
            _db.OfflineQueue.Add(item);
            await _db.SaveChangesAsync();
            GlobalLogger.Instance.LogInfo($"OfflineQueue enqueued: #{item.Id} {channel}/{direction}", "OfflineQueue");
            return item.Id;
        }

        public async Task<OfflineQueueItem?> DequeueNextPendingAsync()
        {
            var now = DateTime.UtcNow;
            var item = await _db.OfflineQueue
                .Where(q => q.Status == "Pending" && (q.NextAttemptAt == null || q.NextAttemptAt <= now))
                .OrderBy(q => q.CreatedDate)
                .FirstOrDefaultAsync();

            if (item != null)
            {
                item.Status = "Processing";
                item.ModifiedDate = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                GlobalLogger.Instance.LogInfo($"OfflineQueue dequeued: #{item.Id}", "OfflineQueue");
            }

            return item;
        }

        public async Task MarkSucceededAsync(int id)
        {
            var item = await _db.OfflineQueue.FindAsync(id);
            if (item == null) return;
            item.Status = "Succeeded";
            item.ModifiedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            GlobalLogger.Instance.LogInfo($"OfflineQueue succeeded: #{id}", "OfflineQueue");
        }

        public async Task MarkFailedAsync(int id, string errorMessage, int? backoffSeconds = null)
        {
            var item = await _db.OfflineQueue.FindAsync(id);
            if (item == null) return;

            item.Status = "Pending"; // tekrar deneme için Pending'e dön
            item.RetryCount += 1;
            item.LastError = errorMessage;
            var baseBackoff = backoffSeconds ?? _options.Retry.BackoffSeconds[Math.Min(item.RetryCount - 1, _options.Retry.BackoffSeconds.Count - 1)];

            // Rate limit (429) tespiti: bekleme süresini büyüt
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                var msg = errorMessage.ToLowerInvariant();
                if (msg.Contains("429") || msg.Contains("too many requests") || msg.Contains("rate limit"))
                {
                    baseBackoff = (int)Math.Ceiling(baseBackoff * 2.0);
                }
            }

            // Jitter (+%0-20) ekle — thundering herd etkisini azaltır
            var jitterMax = Math.Max(1, (int)Math.Ceiling(baseBackoff * 0.2));
            var jitter = new Random().Next(0, jitterMax + 1);
            var effectiveBackoff = baseBackoff + jitter;

            item.NextAttemptAt = DateTime.UtcNow.AddSeconds(effectiveBackoff);
            item.ModifiedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            GlobalLogger.Instance.LogWarning($"OfflineQueue failed: #{id}, retry in {effectiveBackoff}s (base={baseBackoff}, jitter={jitter}), err={errorMessage}", "OfflineQueue");
        }

        public async Task<int> CleanupExpiredAsync()
        {
            var cutoff = DateTime.UtcNow.AddHours(-_options.QueueRetentionHours);
            var expired = await _db.OfflineQueue.Where(q => q.CreatedDate < cutoff && q.Status == "Succeeded").ToListAsync();
            if (expired.Count == 0) return 0;
            _db.OfflineQueue.RemoveRange(expired);
            var deleted = await _db.SaveChangesAsync();
            GlobalLogger.Instance.LogInfo($"OfflineQueue cleanup deleted={deleted}", "OfflineQueue");
            return deleted;
        }
    }
}


