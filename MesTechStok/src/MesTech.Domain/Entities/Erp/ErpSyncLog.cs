using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities.Erp;

/// <summary>
/// ERP senkronizasyon log kaydi — her sync denemesi icin ayri bir kayit tutulur.
/// Basarili/basarisiz sonuc, ERP referansi, retry bilgisi ve hata detayi saklanir.
/// Dalga 11: ERP entegrasyonu icin eklendi.
/// </summary>
public class ErpSyncLog : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>ERP saglayicisi (Logo, Netsis, Parasut vb.).</summary>
    public ErpProvider Provider { get; private set; }

    /// <summary>Sync edilen entity tipi (Order, Invoice vb.).</summary>
    public string EntityType { get; private set; } = string.Empty;

    /// <summary>Sync edilen entity'nin ID'si.</summary>
    public Guid EntityId { get; private set; }

    /// <summary>Sync basarili mi?</summary>
    public bool Success { get; private set; }

    /// <summary>ERP tarafindaki referans numarasi (basarili sync'lerde dolar).</summary>
    public string? ErpRef { get; private set; }

    /// <summary>Hata mesaji (basarisiz sync'lerde dolar).</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>HTTP status kodu (API cagrisinin sonucu).</summary>
    public int HttpStatusCode { get; private set; }

    /// <summary>Retry denemesi sayisi (ilk deneme = 0).</summary>
    public int RetryCount { get; private set; }

    /// <summary>Maksimum retry sayisi.</summary>
    public int MaxRetries { get; private set; } = 3;

    /// <summary>Son deneme zamani (UTC).</summary>
    public DateTime AttemptedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>Sonraki retry zamani (null ise retry yok).</summary>
    public DateTime? NextRetryAt { get; private set; }

    /// <summary>Toplam islenen kayit sayisi.</summary>
    public int TotalRecords { get; private set; }

    /// <summary>Basarili kayit sayisi.</summary>
    public int SuccessCount { get; private set; }

    /// <summary>Basarisiz kayit sayisi.</summary>
    public int FailCount { get; private set; }

    /// <summary>Atlanan kayit sayisi (duplicate vb.).</summary>
    public int SkipCount { get; private set; }

    /// <summary>Sync suresi milisaniye cinsinden.</summary>
    public long DurationMs { get; private set; }

    /// <summary>Detayli hata listesi (JSON format).</summary>
    public string? ErrorDetails { get; private set; }

    /// <summary>Sync tetikleyicisi: "Hangfire" | "Manual" | "Event".</summary>
    public string TriggeredBy { get; private set; } = "Manual";

    /// <summary>Izleme icin korelasyon ID'si.</summary>
    public Guid? CorrelationId { get; private set; }

    // EF Core parametresiz ctor
    private ErpSyncLog() { }

    /// <summary>
    /// Factory method — yeni ERP sync log kaydi olusturur.
    /// </summary>
    public static ErpSyncLog Create(
        Guid tenantId,
        ErpProvider provider,
        string entityType,
        Guid entityId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId bos olamaz.", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("EntityType bos olamaz.", nameof(entityType));

        if (entityId == Guid.Empty)
            throw new ArgumentException("EntityId bos olamaz.", nameof(entityId));

        return new ErpSyncLog
        {
            TenantId = tenantId,
            Provider = provider,
            EntityType = entityType,
            EntityId = entityId,
            Success = false,
            RetryCount = 0,
            AttemptedAt = DateTime.UtcNow
        };
    }

    /// <summary>Sync'i basarili olarak isaretler.</summary>
    public void MarkSuccess(string erpRef, int httpStatusCode = 200)
    {
        if (string.IsNullOrWhiteSpace(erpRef))
            throw new ArgumentException("ErpRef bos olamaz.", nameof(erpRef));

        Success = true;
        ErpRef = erpRef;
        HttpStatusCode = httpStatusCode;
        NextRetryAt = null;
        AttemptedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Sync'i basarisiz olarak isaretler ve retry planlar.</summary>
    public void MarkFailure(string errorMessage, int httpStatusCode = 0)
    {
        Success = false;
        ErrorMessage = errorMessage;
        HttpStatusCode = httpStatusCode;
        RetryCount++;
        AttemptedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        // Exponential backoff: 1m, 5m, 25m...
        if (RetryCount < MaxRetries)
        {
            var delayMinutes = Math.Pow(5, RetryCount);
            NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);
        }
        else
        {
            NextRetryAt = null; // Retry limiti asildi
        }
    }

    /// <summary>Retry sayacini sifirlar (manuel tetikleme icin).</summary>
    public void ResetRetry()
    {
        RetryCount = 0;
        NextRetryAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Sets sync detail metrics after a batch operation completes.</summary>
    public void SetDetails(int totalRecords, int successCount, int failCount, int skipCount,
        long durationMs, string? errorDetails = null, string triggeredBy = "Manual",
        Guid? correlationId = null)
    {
        TotalRecords = totalRecords;
        SuccessCount = successCount;
        FailCount = failCount;
        SkipCount = skipCount;
        DurationMs = durationMs;
        ErrorDetails = errorDetails;
        TriggeredBy = triggeredBy;
        CorrelationId = correlationId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Retry hakkinin olup olmadigini kontrol eder.</summary>
    public bool CanRetry => !Success && RetryCount < MaxRetries;

    public override string ToString() =>
        $"ErpSync [{Provider}] {EntityType}:{EntityId} — {(Success ? $"OK ({ErpRef})" : $"FAIL ({ErrorMessage})")} retry:{RetryCount}/{MaxRetries}";
}
