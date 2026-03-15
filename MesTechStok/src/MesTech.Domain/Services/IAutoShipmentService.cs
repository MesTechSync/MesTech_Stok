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

/// <summary>
/// Kargo atama talebi — siparis bilgilerini tasir.
/// </summary>
public record ShipmentRequest(
    string DestinationCity,
    decimal WeightKg,
    decimal Desi,
    bool IsCashOnDelivery,
    PlatformType? SourcePlatform = null,
    decimal? OrderAmount = null);

/// <summary>
/// Kargo atama onerisi — secilen saglayici ve gerekce.
/// </summary>
public record ShipmentRecommendation(
    CargoProvider Provider,
    string Reason,
    decimal? EstimatedCost = null);
