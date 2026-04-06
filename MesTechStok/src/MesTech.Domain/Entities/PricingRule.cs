using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Platform bazlı otomatik fiyat eşleme kuralı.
/// Örnek kurallar:
///   - "Trendyol'da rakipten 1 TL ucuz ol, minimum %15 margin koru"
///   - "Hepsiburada'da Trendyol fiyatı + %3 komisyon farkı ekle"
///   - "Amazon'da maliyet + %25 markup, minimum 50 TL"
/// </summary>
public sealed class PricingRule : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public PlatformType Platform { get; private set; }
    public Guid? CategoryId { get; private set; }
    public PricingRuleType RuleType { get; private set; }

    // Kural parametreleri
    public decimal? CompetitorOffset { get; private set; }
    public decimal? MinMarginPercent { get; private set; }
    public decimal? MarkupPercent { get; private set; }
    public decimal? MinPrice { get; private set; }
    public decimal? MaxPrice { get; private set; }
    public decimal? RoundTo { get; private set; }

    public bool IsActive { get; private set; }
    public int Priority { get; private set; }

    private PricingRule() { }

    public static PricingRule Create(
        Guid tenantId,
        string name,
        PlatformType platform,
        PricingRuleType ruleType,
        decimal? minMarginPercent = null,
        decimal? competitorOffset = null,
        decimal? markupPercent = null,
        int priority = 0)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new PricingRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Platform = platform,
            RuleType = ruleType,
            MinMarginPercent = minMarginPercent,
            CompetitorOffset = competitorOffset,
            MarkupPercent = markupPercent,
            Priority = priority,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Kuralı uygulayarak hedef fiyatı hesaplar.
    /// </summary>
    public decimal CalculateTargetPrice(decimal costPrice, decimal? competitorPrice)
    {
        if (costPrice <= 0) return 0;

        var target = RuleType switch
        {
            PricingRuleType.CostPlusMarkup =>
                costPrice * (1 + (MarkupPercent ?? 0) / 100m),

            PricingRuleType.CompetitorBased when competitorPrice.HasValue =>
                competitorPrice.Value - (CompetitorOffset ?? 1m),

            PricingRuleType.CompetitorBased =>
                costPrice * (1 + (MarkupPercent ?? 25m) / 100m),

            _ => costPrice
        };

        // Minimum margin koruması
        if (MinMarginPercent.HasValue)
        {
            var floorPrice = costPrice * (1 + MinMarginPercent.Value / 100m);
            if (target < floorPrice)
                target = floorPrice;
        }

        // Min/Max sınırları
        if (MinPrice.HasValue && target < MinPrice.Value)
            target = MinPrice.Value;
        if (MaxPrice.HasValue && target > MaxPrice.Value)
            target = MaxPrice.Value;

        // Yuvarlama
        if (RoundTo.HasValue && RoundTo.Value > 0)
            target = Math.Ceiling(target / RoundTo.Value) * RoundTo.Value - 0.01m;

        return Math.Round(target, 2);
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePriority(int priority)
    {
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }
}
