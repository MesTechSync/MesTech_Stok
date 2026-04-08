using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Platform urun degerlendirmesi — Trendyol, Hepsiburada vb.
/// ReviewSyncJob tarafindan cekilen veriler bu entity'ye persist edilir.
/// </summary>
public sealed class ProductReview : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public PlatformType Platform { get; set; }
    public string ExternalReviewId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public int Rating { get; set; }
    public bool IsReplied { get; set; }
    public string? ReplyText { get; set; }
    public DateTime? RepliedAt { get; set; }
    public DateTime ReviewDate { get; set; }

    // Navigation
    public Product? Product { get; set; }

    private ProductReview() { }

    public static ProductReview Create(
        Guid tenantId,
        Guid productId,
        PlatformType platform,
        string externalReviewId,
        string customerName,
        string comment,
        int rating,
        DateTime reviewDate,
        bool isReplied = false)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
        if (rating is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating 1-5 arası olmalı.");
        ArgumentException.ThrowIfNullOrWhiteSpace(externalReviewId);

        return new ProductReview
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = productId,
            Platform = platform,
            ExternalReviewId = externalReviewId,
            CustomerName = customerName,
            Comment = comment,
            Rating = rating,
            ReviewDate = reviewDate,
            IsReplied = isReplied,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsReplied(string replyText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replyText);
        IsReplied = true;
        ReplyText = replyText;
        RepliedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
