using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Urun gorseli/videosu — coklu medya destegi (Trendyol 8 gorsel, HB 10, Amazon 9).
/// </summary>
public sealed class ProductMedia : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public MediaType Type { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string? ThumbnailUrl { get; private set; }
    public string? AltText { get; private set; }
    public int SortOrder { get; private set; }
    public int? DurationSeconds { get; private set; }

    // Navigation
    public Product? Product { get; set; }
    public ProductVariant? Variant { get; set; }

    private ProductMedia() { }

    public static ProductMedia Create(
        Guid tenantId, Guid productId, MediaType type, string url, int sortOrder,
        Guid? variantId = null, string? altText = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId bos olamaz.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        return new ProductMedia
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = productId,
            VariantId = variantId,
            Type = type,
            Url = url,
            AltText = altText,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetThumbnail(string thumbnailUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thumbnailUrl);
        ThumbnailUrl = thumbnailUrl;
    }

    public void SetDuration(int seconds)
    {
        if (seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        DurationSeconds = seconds;
    }

    public void UpdateSortOrder(int order) => SortOrder = order;
}

public enum MediaType
{
    Image = 0,
    Video = 1,
    Video360 = 2,
    SizeChart = 3,
    Certificate = 4,
    TechnicalDrawing = 5,
    LifestyleImage = 6,
    PackageImage = 7
}
