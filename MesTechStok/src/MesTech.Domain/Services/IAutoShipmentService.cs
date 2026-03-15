using MesTech.Domain.Enums;

namespace MesTech.Domain.Services;

/// <summary>
/// Otomatik kargo atama servisi arayuzu.
/// Siparis bilgilerine gore en uygun kargo firmasini belirler.
/// </summary>
public interface IAutoShipmentService
{
    /// <summary>
    /// Siparis bilgilerine gore en uygun kargo saglayiciyi oner.
    /// Karar kriterleri: hedef sehir, agirlik/desi, kapida odeme, platform tercihi.
    /// </summary>
    ShipmentRecommendation Recommend(ShipmentRequest request);
}
