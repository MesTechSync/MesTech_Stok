using MesTech.Domain.Common;
using MesTech.Domain.Constants;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Sosyal ticaret feed yapilandirmasi — bir tenant'in bir platforma yonelik feed ayarlari.
/// Google Merchant, Facebook Shop, Akakce, Cimri vb. platformlara urun feed'i uretir.
/// </summary>
public sealed class SocialFeedConfiguration : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public SocialFeedPlatform Platform { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Uretilen feed URL'si. Ilk generation oncesi null.</summary>
    public string? FeedUrl { get; set; }

    /// <summary>Feed yenileme periyodu.</summary>
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromHours(6);

    /// <summary>Son feed uretim zamani (UTC).</summary>
    public DateTime? LastGeneratedAt { get; set; }

    /// <summary>Son uretimde feed'e dahil edilen urun sayisi.</summary>
    public int ItemCount { get; set; }

    /// <summary>Son uretimde olusan hata mesaji. Basarili uretimde null.</summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Virgul ile ayrilmis kategori filtresi. null: tum kategoriler dahil.
    /// Ornek: "Elektronik,Giyim,Kozmetik"
    /// </summary>
    public string? CategoryFilter { get; set; }

    // EF Core parametresiz ctor
    private SocialFeedConfiguration() { }

    /// <summary>
    /// Factory method — yeni feed yapilandirmasi olusturur.
    /// </summary>
    public static SocialFeedConfiguration Create(
        Guid tenantId,
        SocialFeedPlatform platform,
        TimeSpan? refreshInterval = null,
        string? categoryFilter = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId bos olamaz.", nameof(tenantId));

        if (!Enum.IsDefined(platform))
            throw new ArgumentOutOfRangeException(nameof(platform), "Gecersiz platform degeri.");

        return new SocialFeedConfiguration
        {
            TenantId = tenantId,
            Platform = platform,
            RefreshInterval = refreshInterval ?? TimeSpan.FromHours(6),
            CategoryFilter = categoryFilter,
            IsActive = true
        };
    }

    /// <summary>Feed uretim sonucunu kaydeder.</summary>
    public void RecordGeneration(string feedUrl, int itemCount, string? error = null)
    {
        FeedUrl = feedUrl;
        ItemCount = itemCount;
        LastGeneratedAt = DateTime.UtcNow;
        LastError = error;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Feed uretim hatasini kaydeder.</summary>
    public void RecordError(string errorMessage)
    {
        LastError = DomainConstants.Truncate(errorMessage, DomainConstants.MaxErrorMessageLength);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Bir sonraki planlanan uretim zamani.</summary>
    public DateTime? NextScheduledGeneration => LastGeneratedAt.HasValue
        ? LastGeneratedAt.Value.Add(RefreshInterval)
        : null;

    /// <summary>Feed'in yenilenmesi gerekip gerekmedigi.</summary>
    public bool NeedsRefresh => !LastGeneratedAt.HasValue
        || DateTime.UtcNow >= LastGeneratedAt.Value.Add(RefreshInterval);

    public override string ToString() =>
        $"SocialFeed [{Platform}] Active:{IsActive} Items:{ItemCount} Last:{LastGeneratedAt:u}";
}
