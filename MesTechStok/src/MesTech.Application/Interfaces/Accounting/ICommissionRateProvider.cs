using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces.Accounting;

/// <summary>
/// Dinamik komisyon orani saglayicisi — platform API'lerinden guncel orani ceker.
/// DEV 3 tarafindan implement edilecektir.
/// </summary>
public interface ICommissionRateProvider
{
    Task<CommissionRateInfo?> GetRateAsync(
        string platform,
        string? categoryId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Komisyon orani bilgisi — platform, kategori ve kaynak bilgisi ile birlikte.
/// </summary>
public record CommissionRateInfo(
    decimal Rate,
    CommissionType Type,
    string Source,        // "TrendyolAPI", "HepsiburadaAPI", "StaticFallback"
    DateTime CachedUntil  // Cache validity
);
