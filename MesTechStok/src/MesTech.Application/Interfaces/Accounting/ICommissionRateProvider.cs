using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces.Accounting;

/// <summary>
/// Dinamik komisyon orani saglayicisi — platform API'lerinden guncel orani ceker.
/// DEV 3 tarafindan implement edilecektir.
/// </summary>
public interface ICommissionRateProvider
{
    /// <summary>
    /// Legacy overload — tenant-agnostic. Kullanmayın, tenantId overload'u tercih edin.
    /// </summary>
    Task<CommissionRateInfo?> GetRateAsync(
        string platform,
        string? categoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Multi-tenant overload — settlement repo tenant-filtered query kullanır.
    /// DEV3: implementasyonda bu overload'u override edin.
    /// </summary>
    Task<CommissionRateInfo?> GetRateAsync(
        Guid tenantId,
        string platform,
        string? categoryId,
        CancellationToken cancellationToken = default)
    {
        // Default: legacy overload'a yönlendir. DEV3 override edecek.
        return GetRateAsync(platform, categoryId, cancellationToken);
    }
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
