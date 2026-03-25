using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;

namespace MesTech.Domain.Entities;

/// <summary>
/// Tedarikçi ürün feed tanımı (dropshipping).
/// Hangfire job periyodik olarak feed URL'den çeker, parse eder, fiyat markup uygular.
/// Stok sıfır olan ürünler otomatik pasife alınır.
/// </summary>
public sealed class SupplierFeed : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid SupplierId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string FeedUrl { get; set; } = string.Empty;
    public FeedFormat Format { get; set; } = FeedFormat.None;

    // Fiyat markup ayarları
    public decimal PriceMarkupPercent { get; set; }
    public decimal PriceMarkupFixed { get; set; }
    public bool UsePercentMarkup { get; set; } = true;

    // Otomasyon ayarları
    public bool AutoDeactivateOnZeroStock { get; set; } = true;
    public bool AutoActivateOnRestock { get; set; } = true;
    public bool IsActive { get; set; } = true;

    // Sync periyodu (cron expression veya dakika cinsinden)
    public int SyncIntervalMinutes { get; set; } = 60;
    public string? CronExpression { get; set; }

    // Son sync durumu — RecordSyncResult() ile yönetilir
    public FeedSyncStatus LastSyncStatus { get; private set; } = FeedSyncStatus.None;
    public DateTime? LastSyncAt { get; private set; }
    public int LastSyncProductCount { get; private set; }
    public int LastSyncUpdatedCount { get; private set; }
    public int LastSyncDeactivatedCount { get; private set; }
    public string? LastSyncError { get; private set; }

    // Hedef platformlar (virgülle ayrılmış PlatformType değerleri)
    public string? TargetPlatforms { get; set; }

    // ENT-DROP-IMP-SPRINT-D D-07: Şifrelenmiş HTTP Basic Auth credential
    // Repoda asla plain-text gözükmez; IFeedCredentialProtector ile yönetilir.
    // EF Core backing field pattern: private setter, public read.

    /// <summary>
    /// AES-256-GCM ile şifrelenmiş credential blob.
    /// Plain-text asla bu alana yazılmaz — IFeedCredentialProtector.Protect() çıktısı gelir.
    /// </summary>
    public string? EncryptedCredential { get; private set; }

    /// <summary>Feed için credential tanımlı mı?</summary>
    public bool HasCredential => !string.IsNullOrEmpty(EncryptedCredential);

    /// <summary>Şifrelenmiş credential'ı set eder (şifreli değer dışarıdan gelir).</summary>
    public void SetCredential(string? encryptedValue)
    {
        EncryptedCredential = encryptedValue;
        UpdatedAt = DateTime.UtcNow;
    }

    // Navigation
    public Supplier? Supplier { get; set; }

    public void MarkSyncInProgress()
    {
        LastSyncStatus = FeedSyncStatus.InProgress;
    }

    public decimal ApplyMarkup(decimal originalPrice)
    {
        if (UsePercentMarkup)
            return originalPrice * (1 + PriceMarkupPercent / 100m);
        return originalPrice + PriceMarkupFixed;
    }

    public void RecordSyncResult(int totalProducts, int updatedProducts, int deactivatedProducts, string? error = null)
    {
        if (totalProducts < 0)
            throw new ArgumentOutOfRangeException(nameof(totalProducts), "Total products cannot be negative.");
        if (updatedProducts < 0)
            throw new ArgumentOutOfRangeException(nameof(updatedProducts), "Updated products cannot be negative.");
        if (deactivatedProducts < 0)
            throw new ArgumentOutOfRangeException(nameof(deactivatedProducts), "Deactivated products cannot be negative.");
        LastSyncAt = DateTime.UtcNow;
        LastSyncProductCount = totalProducts;
        LastSyncUpdatedCount = updatedProducts;
        LastSyncDeactivatedCount = deactivatedProducts;
        LastSyncError = error;
        LastSyncStatus = error != null ? FeedSyncStatus.PartiallyCompleted : FeedSyncStatus.Completed;

        RaiseDomainEvent(new SupplierFeedSyncedEvent(
            Id, TenantId, SupplierId, totalProducts, updatedProducts, deactivatedProducts,
            LastSyncStatus, DateTime.UtcNow));
    }

    public override string ToString() => $"Feed [{Name}] ({Format}) - {LastSyncStatus}";
}
