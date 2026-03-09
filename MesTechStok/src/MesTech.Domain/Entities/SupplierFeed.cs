using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;

namespace MesTech.Domain.Entities;

/// <summary>
/// Tedarikçi ürün feed tanımı (dropshipping).
/// Hangfire job periyodik olarak feed URL'den çeker, parse eder, fiyat markup uygular.
/// Stok sıfır olan ürünler otomatik pasife alınır.
/// </summary>
public class SupplierFeed : BaseEntity, ITenantEntity
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

    // Son sync durumu
    public FeedSyncStatus LastSyncStatus { get; set; } = FeedSyncStatus.None;
    public DateTime? LastSyncAt { get; set; }
    public int LastSyncProductCount { get; set; }
    public int LastSyncUpdatedCount { get; set; }
    public int LastSyncDeactivatedCount { get; set; }
    public string? LastSyncError { get; set; }

    // Hedef platformlar (virgülle ayrılmış PlatformType değerleri)
    public string? TargetPlatforms { get; set; }

    // Navigation
    public Supplier? Supplier { get; set; }

    public decimal ApplyMarkup(decimal originalPrice)
    {
        if (UsePercentMarkup)
            return originalPrice * (1 + PriceMarkupPercent / 100m);
        return originalPrice + PriceMarkupFixed;
    }

    public void RecordSyncResult(int totalProducts, int updatedProducts, int deactivatedProducts, string? error = null)
    {
        LastSyncAt = DateTime.UtcNow;
        LastSyncProductCount = totalProducts;
        LastSyncUpdatedCount = updatedProducts;
        LastSyncDeactivatedCount = deactivatedProducts;
        LastSyncError = error;
        LastSyncStatus = error != null ? FeedSyncStatus.PartiallyCompleted : FeedSyncStatus.Completed;

        RaiseDomainEvent(new SupplierFeedSyncedEvent(
            Id, SupplierId, totalProducts, updatedProducts, deactivatedProducts,
            LastSyncStatus, DateTime.UtcNow));
    }

    public override string ToString() => $"Feed [{Name}] ({Format}) - {LastSyncStatus}";
}
