using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Gelen webhook isteklerinin denetim kaydi.
/// Imza dogrulama sonucu, payload ve hata bilgisi saklar.
/// Retry mekanizmasi icin IsValid=false kayitlar kullanilir.
/// </summary>
public class WebhookLog : ITenantEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; set; }
    public string Platform { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public string? Signature { get; private set; }
    public bool IsValid { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }

    /// <summary>ORM icin parametresiz constructor.</summary>
    private WebhookLog() { }

    /// <summary>
    /// Yeni webhook log kaydi olusturur.
    /// </summary>
    public static WebhookLog Create(
        Guid tenantId,
        string platform,
        string eventType,
        string payload,
        string? signature,
        bool isValid,
        string? error = null)
    {
        return new WebhookLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Platform = platform,
            EventType = eventType,
            Payload = payload,
            Signature = signature,
            IsValid = isValid,
            ReceivedAt = DateTime.UtcNow,
            ProcessedAt = isValid ? DateTime.UtcNow : null,
            Error = error,
            RetryCount = 0
        };
    }

    /// <summary>Retry sonrasi basarili isleme.</summary>
    public void MarkProcessed()
    {
        IsValid = true;
        ProcessedAt = DateTime.UtcNow;
        Error = null;
    }

    /// <summary>Retry sayacini artir ve hatayi guncelle.</summary>
    public void IncrementRetry(string? error)
    {
        RetryCount++;
        Error = error;
    }
}
