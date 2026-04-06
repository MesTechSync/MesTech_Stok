using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Services;

/// <summary>
/// Sipariş SLA (Service Level Agreement) takip servisi.
/// Platform bazlı kargoya verme süresi kontrolü.
///
/// Trendyol: Onaydan itibaren 24 saat (standart), 48 saat (büyük ürün)
/// Hepsiburada: 48 saat
/// N11: 72 saat
/// Amazon: 24 saat (FBA hariç)
/// Ciceksepeti: 24 saat
///
/// SLA ihlali → platform ceza puanı → mağaza askıya alma riski
/// </summary>
public sealed class OrderSlaService
{
    private static readonly Dictionary<PlatformType, TimeSpan> _slaLimits = new()
    {
        [PlatformType.Trendyol] = TimeSpan.FromHours(24),
        [PlatformType.Hepsiburada] = TimeSpan.FromHours(48),
        [PlatformType.N11] = TimeSpan.FromHours(72),
        [PlatformType.Amazon] = TimeSpan.FromHours(24),
        [PlatformType.AmazonEu] = TimeSpan.FromHours(24),
        [PlatformType.Ciceksepeti] = TimeSpan.FromHours(24),
        [PlatformType.eBay] = TimeSpan.FromHours(72),
        [PlatformType.Etsy] = TimeSpan.FromHours(72),
        [PlatformType.Pazarama] = TimeSpan.FromHours(48),
        [PlatformType.PttAVM] = TimeSpan.FromHours(48),
    };

    /// <summary>
    /// Sipariş SLA durumunu hesaplar.
    /// </summary>
    public OrderSlaStatus CheckSla(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.Status != OrderStatus.Confirmed)
            return new OrderSlaStatus(SlaState.NotApplicable, TimeSpan.Zero, TimeSpan.Zero, null);

        if (!order.SourcePlatform.HasValue)
            return new OrderSlaStatus(SlaState.NotApplicable, TimeSpan.Zero, TimeSpan.Zero, null);

        var slaLimit = GetSlaLimit(order.SourcePlatform.Value);
        var elapsed = DateTime.UtcNow - (order.ConfirmedAt ?? order.OrderDate);
        var remaining = slaLimit - elapsed;

        if (remaining <= TimeSpan.Zero)
            return new OrderSlaStatus(SlaState.Violated, slaLimit, elapsed, remaining);

        if (remaining <= TimeSpan.FromHours(4))
            return new OrderSlaStatus(SlaState.Warning, slaLimit, elapsed, remaining);

        return new OrderSlaStatus(SlaState.OnTrack, slaLimit, elapsed, remaining);
    }

    /// <summary>
    /// Platform için SLA süresini döndürür.
    /// Bilinmeyen platform → 72 saat (güvenli varsayılan).
    /// </summary>
    public TimeSpan GetSlaLimit(PlatformType platform)
        => _slaLimits.TryGetValue(platform, out var limit) ? limit : TimeSpan.FromHours(72);
}

public record OrderSlaStatus(
    SlaState State,
    TimeSpan SlaLimit,
    TimeSpan Elapsed,
    TimeSpan? Remaining);

public enum SlaState
{
    NotApplicable,
    OnTrack,
    Warning,
    Violated
}
