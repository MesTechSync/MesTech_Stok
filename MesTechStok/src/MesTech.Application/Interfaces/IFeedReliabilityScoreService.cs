namespace MesTech.Application.Interfaces;

/// <summary>
/// Tedarikçi feed güvenilirlik skor hesaplama arayüzü.
/// Bileşenler: StockAccuracy(25%), UpdateFrequency(20%), FeedAvailability(20%),
/// ProductStability(20%), ResponseTime(15%).
/// Çıktı: 0-100 puan + ReliabilityColor.
/// </summary>
public interface IFeedReliabilityScoreService
{
    Task<SupplierReliabilityScore> CalculateAsync(Guid supplierFeedId, CancellationToken ct = default);
}

public record SupplierReliabilityScore(
    Guid SupplierFeedId,
    int Score,
    ReliabilityColor Color,
    decimal StockAccuracy,
    decimal UpdateFrequency,
    decimal FeedAvailability,
    decimal ProductStability,
    decimal ResponseTime);

public enum ReliabilityColor
{
    Red = 0,    // 0-49
    Orange = 1, // 50-74
    Yellow = 2, // 75-89
    Green = 3   // 90-100
}
