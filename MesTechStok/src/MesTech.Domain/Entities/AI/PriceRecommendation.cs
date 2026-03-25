using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.AI;

/// <summary>
/// AI fiyat onerisi history — her oneri kaydedilir, dogruluk takibi yapilir.
/// </summary>
public sealed class PriceRecommendation : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public decimal RecommendedPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal? CompetitorMinPrice { get; set; }
    public double Confidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>"ai.price.recommended" veya "ai.price.optimized"</summary>
    public string Source { get; set; } = string.Empty;

    public string? Strategy { get; set; }
    public DateTime? AppliedAt { get; set; }
    public bool? WasAccepted { get; set; }
}
