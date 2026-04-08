using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// WebhookDeadLetter retry job — DLQ'daki pending webhook'ları exponential backoff ile retry eder.
/// Her 5 dakika çalışır. NextRetryAt &lt; now olan kayıtları çeker, IWebhookProcessor ile tekrar işler.
/// Entity: WebhookDeadLetter (RecordRetry ile backoff hesaplar).
/// Max 5 attempt — sonra Failed status'e geçer.
/// </summary>
[AutomaticRetry(Attempts = 2)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class WebhookRetryJob : ISyncJob
{
    public string JobId => "webhook-dlq-retry";
    public string CronExpression => "*/5 * * * *"; // Her 5 dakika

    private readonly IWebhookProcessor _processor;
    private readonly IWebhookDeadLetterRepository _dlqRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WebhookRetryJob> _logger;
    private const int BatchSize = 20;

    public WebhookRetryJob(
        IWebhookProcessor processor,
        IWebhookDeadLetterRepository dlqRepo,
        IUnitOfWork unitOfWork,
        ILogger<WebhookRetryJob> logger)
    {
        _processor = processor;
        _dlqRepo = dlqRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Webhook DLQ retry başladı...", JobId);

        try
        {
            var pendingItems = await _dlqRepo.GetPendingRetryAsync(DateTime.UtcNow, ct)
                .ConfigureAwait(false);

            if (pendingItems.Count == 0)
            {
                _logger.LogDebug("[{JobId}] DLQ'da retry bekleyen kayıt yok.", JobId);
                return;
            }

            _logger.LogInformation("[{JobId}] {Count} webhook retry edilecek", JobId, pendingItems.Count);

            var retried = 0;
            var succeeded = 0;

            foreach (var item in pendingItems.Take(BatchSize))
            {
                try
                {
                    var result = await _processor.ProcessAsync(
                        item.Platform, item.RawBody, item.Signature, ct).ConfigureAwait(false);

                    item.RecordRetry(result.Success, result.Success ? null : result.Error);
                    retried++;
                    if (result.Success) succeeded++;

                    _logger.LogInformation(
                        "[{JobId}] Retry {Status}: Platform={Platform}, Attempt={Attempt}/{Max}",
                        JobId, result.Success ? "OK" : "FAIL",
                        item.Platform, item.AttemptCount, item.MaxAttempts);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    item.RecordRetry(false, ex.Message);
                    retried++;

                    _logger.LogWarning(ex,
                        "[{JobId}] Retry exception: Platform={Platform}, Attempt={Attempt}",
                        JobId, item.Platform, item.AttemptCount);
                }

                // Idempotency guard (G086): per-item save — crash durumunda
                // RecordRetry sonucu kaybolmaz, aynı webhook tekrar denenmez
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "[{JobId}] DLQ retry tamamlandı: {Retried} işlendi, {Succeeded} başarılı",
                JobId, retried, succeeded);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[{JobId}] Webhook DLQ retry HATA", JobId);
            throw;
        }
    }
}
