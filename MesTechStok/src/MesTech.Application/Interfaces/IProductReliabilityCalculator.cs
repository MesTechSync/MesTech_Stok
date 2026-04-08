namespace MesTech.Application.Interfaces;

/// <summary>
/// 4 boyutlu ürün güvenilirlik hesaplayıcı (D12-24).
/// Boyut 1: Tedarikçi (mevcut IFeedReliabilityScoreService delegate)
/// Boyut 2: Ürün Kalitesi (iade oranı, şikayet, puan)
/// Boyut 3: Satış Performansı (satış hızı, stok tutarlılık)
/// Boyut 4: Kargo & Lojistik (teslimat süresi, hasar oranı)
/// </summary>
public interface IProductReliabilityCalculator
{
    ProductReliabilityResult Calculate(ProductReliabilityInput input);
}

public record ProductReliabilityInput(
    decimal SupplierReliabilityScore,
    decimal ReturnRate,
    decimal ComplaintRate,
    decimal AverageRating,
    int TotalReviews,
    int SalesLast30Days,
    decimal StockConsistencyRate,
    decimal AverageDeliveryDays,
    decimal DamageRate,
    decimal OnTimeDeliveryRate);

public record ProductReliabilityResult(
    decimal OverallScore,
    ReliabilityColor OverallColor,
    decimal SupplierScore,
    ReliabilityColor SupplierColor,
    decimal QualityScore,
    ReliabilityColor QualityColor,
    decimal SalesScore,
    ReliabilityColor SalesColor,
    decimal LogisticsScore,
    ReliabilityColor LogisticsColor);
