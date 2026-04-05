namespace MesTech.Domain.Enums;

/// <summary>
/// Otomatik fiyatlama kural tipi.
/// </summary>
public enum PricingRuleType
{
    /// <summary>Maliyet + markup yüzdesi</summary>
    CostPlusMarkup = 0,

    /// <summary>Rakip fiyatına göre (offset ile)</summary>
    CompetitorBased = 1,

    /// <summary>Platform bazlı komisyon farkı ekleme</summary>
    PlatformCommissionAdjust = 2,

    /// <summary>Manuel sabit fiyat (override)</summary>
    FixedPrice = 3
}
