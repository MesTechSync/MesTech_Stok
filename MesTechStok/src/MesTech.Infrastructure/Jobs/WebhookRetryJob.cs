using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Basarisiz webhook'lari artan araliklarla tekrar dener.
/// Retry araliklari: 1m, 5m, 30m (max 3 retry).
/// Hangfire recurring job olarak calisir.
/// </summary>
[AutomaticRetry(Attempts = 3)]
public class WebhookRetryJob : ISyncJob
{
    public string JobId => "webhook-retry";
    public string CronExpression => "* * * * *"; // Her dakika kontrol

    private readonly IWebhookProcessor _processor;
    private readonly ILogger<WebhookRetryJob> _logger;

    /// <summary>
    /// Retry araliklari (dakika). Her retry icin minimum bekleme suresi.
    /// RetryCount 0 → 1 dakika sonra, 1 → 5 dakika sonra, 2 → 30 dakika sonra.
    /// </summary>
    private static readonly int[] RetryDelayMinutes = [1, 5, 30];
    private const int MaxRetryCount = 3;

    public WebhookRetryJob(
        IWebhookProcessor processor,
        ILogger<WebhookRetryJob> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Webhook retry job basladi...", JobId);

        try
        {
            // Future: AppDbContext'e WebhookLog DbSet eklenince:
            // 1. IsValid=false && RetryCount < MaxRetryCount kayitlarini cek
            // 2. Her kayit icin retry delay'i kontrol et:
            //    - RetryCount < RetryDelayMinutes.Length → RetryDelayMinutes[RetryCount] dakika bekle
            //    - ReceivedAt + delay < DateTime.UtcNow ise retry yap
            // 3. IWebhookProcessor.ProcessAsync ile tekrar isle
            // 4. Basarili ise WebhookLog.MarkProcessed(), basarisiz ise IncrementRetry(error)
            //
            // Ornek:
            // var failedLogs = await _dbContext.WebhookLogs
            //     .Where(w => !w.IsValid && w.RetryCount < MaxRetryCount)
            //     .OrderBy(w => w.ReceivedAt)
            //     .Take(50)
            //     .ToListAsync(ct);
            //
            // foreach (var log in failedLogs)
            // {
            //     var delayIndex = Math.Min(log.RetryCount, RetryDelayMinutes.Length - 1);
            //     var minDelay = TimeSpan.FromMinutes(RetryDelayMinutes[delayIndex]);
            //     if (DateTime.UtcNow - log.ReceivedAt < minDelay) continue;
            //
            //     var result = await _processor.ProcessAsync(
            //         log.Platform, log.Payload, log.Signature, ct);
            //
            //     if (result.Success)
            //         log.MarkProcessed();
            //     else
            //         log.IncrementRetry(result.Error);
            //
            //     await _dbContext.SaveChangesAsync(ct);
            // }

            _logger.LogInformation("[{JobId}] Webhook retry tamamlandi.", JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobId}] Webhook retry HATA", JobId);
            throw;
        }
    }
}
