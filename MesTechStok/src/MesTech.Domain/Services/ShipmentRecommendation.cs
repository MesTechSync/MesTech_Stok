using MesTech.Domain.Enums;

namespace MesTech.Domain.Services;

/// <summary>
/// Kargo atama onerisi — secilen saglayici ve gerekce.
/// </summary>
public record ShipmentRecommendation(
    CargoProvider Provider,
    string Reason,
    decimal? EstimatedCost = null);
