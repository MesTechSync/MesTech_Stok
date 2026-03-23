using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Fiyat değişiklik geçmişi — ürün fiyat takibi ve analiz.
/// </summary>
public class PriceHistory : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public PlatformType? Platform { get; set; }
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public decimal? OldListPrice { get; set; }
    public decimal? NewListPrice { get; set; }
    public string? Currency { get; set; } = "TRY";
    public string? ChangedBy { get; set; }
    public string? ChangeReason { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Product Product { get; set; } = null!;

    public decimal PriceChangePercent =>
        OldPrice != 0 ? Math.Round((NewPrice - OldPrice) / OldPrice * 100, 2) : 0;
}
