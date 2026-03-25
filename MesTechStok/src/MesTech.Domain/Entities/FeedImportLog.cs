using MesTech.Domain.Common;
using MesTech.Domain.Constants;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Tedarikçi feed import geçmiş kaydı.
/// Her sync denemesi için ayrı bir kayıt tutulur.
/// SupplierFeed.LastSync* alanları sadece son sync'i saklarken bu entity tüm geçmişi korur.
/// </summary>
public sealed class FeedImportLog : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>Bağlı tedarikçi feed tanımı.</summary>
    public Guid SupplierFeedId { get; private set; }

    /// <summary>Import işleminin başladığı zaman (UTC).</summary>
    public DateTime StartedAt { get; private set; }

    /// <summary>Import işleminin tamamlandığı zaman (UTC). null: hâlâ devam ediyor veya çöktü.</summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>Import sonuç durumu.</summary>
    public FeedSyncStatus Status { get; private set; } = FeedSyncStatus.InProgress;

    /// <summary>Feed'deki toplam ürün sayısı.</summary>
    public int TotalProducts { get; private set; }

    /// <summary>Yeni eklenen ürün sayısı.</summary>
    public int NewProducts { get; private set; }

    /// <summary>Güncellenen ürün sayısı (stok/fiyat vb.).</summary>
    public int UpdatedProducts { get; private set; }

    /// <summary>Pasife alınan ürün sayısı (stok sıfır olanlar).</summary>
    public int DeactivatedProducts { get; private set; }

    /// <summary>Hata mesajı. max 2000 karakter. Başarılı import'larda null.</summary>
    public string? ErrorMessage { get; private set; }

    // Navigation
    public SupplierFeed? Feed { get; private set; }

    // EF Core parametresiz ctor
    private FeedImportLog() { }

    public FeedImportLog(Guid tenantId, Guid supplierFeedId)
    {
        if (supplierFeedId == Guid.Empty)
            throw new ArgumentException("SupplierFeedId boş olamaz.", nameof(supplierFeedId));

        TenantId = tenantId;
        SupplierFeedId = supplierFeedId;
        StartedAt = DateTime.UtcNow;
        Status = FeedSyncStatus.InProgress;
    }

    /// <summary>Import başarıyla tamamlandığında çağrılır.</summary>
    public void Complete(int totalProducts, int newProducts, int updatedProducts, int deactivatedProducts)
    {
        TotalProducts = totalProducts;
        NewProducts = newProducts;
        UpdatedProducts = updatedProducts;
        DeactivatedProducts = deactivatedProducts;
        CompletedAt = DateTime.UtcNow;
        Status = FeedSyncStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Import kısmen tamamlandığında çağrılır (bazı ürünlerde hata vardı).</summary>
    public void CompletePartially(int totalProducts, int newProducts, int updatedProducts,
        int deactivatedProducts, string errorMessage)
    {
        TotalProducts = totalProducts;
        NewProducts = newProducts;
        UpdatedProducts = updatedProducts;
        DeactivatedProducts = deactivatedProducts;
        ErrorMessage = DomainConstants.Truncate(errorMessage, DomainConstants.MaxErrorMessageLength);
        CompletedAt = DateTime.UtcNow;
        Status = FeedSyncStatus.PartiallyCompleted;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Import başarısız olduğunda çağrılır.</summary>
    public void Fail(string errorMessage)
    {
        ErrorMessage = DomainConstants.Truncate(errorMessage, DomainConstants.MaxErrorMessageLength);
        CompletedAt = DateTime.UtcNow;
        Status = FeedSyncStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Import süresini hesaplar.</summary>
    public TimeSpan? Duration => CompletedAt.HasValue
        ? CompletedAt.Value - StartedAt
        : null;

    public override string ToString() =>
        $"FeedImportLog [Feed:{SupplierFeedId}] {Status} — Total:{TotalProducts} New:{NewProducts} Updated:{UpdatedProducts}";
}
