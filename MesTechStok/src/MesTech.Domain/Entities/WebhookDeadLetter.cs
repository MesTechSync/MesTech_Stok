using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Webhook Dead Letter — başarısız webhook'ları persist eder.
/// Hangfire job ile exponential backoff retry (max 5 attempt).
/// Hem payment webhook hem marketplace webhook failure'ları kapsar.
/// Production'da para/sipariş kaybını önler.
/// </summary>
public sealed class WebhookDeadLetter : BaseEntity, ITenantEntity
{
    public string Platform { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string RawBody { get; private set; } = string.Empty;
    public string? Signature { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; private set; } = 5;
    public DateTime? LastAttemptAt { get; private set; }
    public DateTime? NextRetryAt { get; private set; }
    public WebhookDeadLetterStatus Status { get; private set; }
    public string? ProcessedBy { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    private WebhookDeadLetter() { }

    public static WebhookDeadLetter Create(
        string platform, string eventType, string rawBody,
        string? signature, string errorMessage)
    {
        var now = DateTime.UtcNow;
        return new WebhookDeadLetter
        {
            Id = Guid.NewGuid(),
            Platform = platform,
            EventType = eventType,
            RawBody = rawBody,
            Signature = signature,
            ErrorMessage = errorMessage,
            AttemptCount = 1,
            LastAttemptAt = now,
            NextRetryAt = now.AddMinutes(1), // İlk retry: 1 dakika
            Status = WebhookDeadLetterStatus.Pending,
            CreatedAt = now
        };
    }

    /// <summary>Retry attempt kaydı — exponential backoff.</summary>
    public void RecordRetry(bool success, string? errorMessage = null)
    {
        AttemptCount++;
        LastAttemptAt = DateTime.UtcNow;
        ErrorMessage = errorMessage ?? ErrorMessage;

        if (success)
        {
            Status = WebhookDeadLetterStatus.Resolved;
            ResolvedAt = DateTime.UtcNow;
            NextRetryAt = null;
        }
        else if (AttemptCount >= MaxAttempts)
        {
            Status = WebhookDeadLetterStatus.Failed;
            NextRetryAt = null;
        }
        else
        {
            // Exponential backoff: 1m, 5m, 15m, 60m
            var delayMinutes = AttemptCount switch
            {
                2 => 5,
                3 => 15,
                4 => 60,
                _ => 120
            };
            NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Admin tarafından manuel çözüldü olarak işaretle.</summary>
    public void MarkManuallyResolved(string processedBy)
    {
        Status = WebhookDeadLetterStatus.ManuallyResolved;
        ProcessedBy = processedBy;
        ResolvedAt = DateTime.UtcNow;
        NextRetryAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum WebhookDeadLetterStatus
{
    Pending = 0,
    Resolved = 1,
    Failed = 2,
    ManuallyResolved = 3
}
