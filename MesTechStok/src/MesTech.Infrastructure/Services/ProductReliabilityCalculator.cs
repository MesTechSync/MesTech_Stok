using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// 4 boyutlu ürün güvenilirlik hesaplayıcı (D12-24).
/// Ağırlıklar: Tedarikçi 30%, Kalite 25%, Satış 25%, Lojistik 20%.
/// Renk eşikleri: ≥90 Green, ≥70 Yellow, ≥50 Orange, &lt;50 Red.
/// </summary>
public sealed class ProductReliabilityCalculator : IProductReliabilityCalculator
{
    public ProductReliabilityResult Calculate(ProductReliabilityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var supplierScore = Math.Clamp(input.SupplierReliabilityScore, 0, 100);
        var qualityScore = CalculateQualityScore(input);
        var salesScore = CalculateSalesScore(input);
        var logisticsScore = CalculateLogisticsScore(input);

        var overall = supplierScore * 0.30m + qualityScore * 0.25m
                    + salesScore * 0.25m + logisticsScore * 0.20m;

        return new ProductReliabilityResult(
            OverallScore: Math.Round(overall, 1),
            OverallColor: GetColor(overall),
            SupplierScore: Math.Round(supplierScore, 1),
            SupplierColor: GetColor(supplierScore),
            QualityScore: Math.Round(qualityScore, 1),
            QualityColor: GetColor(qualityScore),
            SalesScore: Math.Round(salesScore, 1),
            SalesColor: GetColor(salesScore),
            LogisticsScore: Math.Round(logisticsScore, 1),
            LogisticsColor: GetColor(logisticsScore));
    }

    private static decimal CalculateQualityScore(ProductReliabilityInput input)
    {
        // İade oranı: %0 = 100, %5+ = 0
        var returnScore = Math.Max(0, 100 - input.ReturnRate * 20);

        // Şikayet oranı: %0 = 100, %3+ = 0
        var complaintScore = Math.Max(0, 100 - input.ComplaintRate * 33.33m);

        // Ortalama puan: 5.0 = 100, 1.0 = 0
        var ratingScore = input.TotalReviews >= 5
            ? Math.Clamp((input.AverageRating - 1m) / 4m * 100m, 0, 100)
            : 50m; // Yeterli değerlendirme yoksa nötr

        return returnScore * 0.40m + complaintScore * 0.30m + ratingScore * 0.30m;
    }

    private static decimal CalculateSalesScore(ProductReliabilityInput input)
    {
        // Satış hızı: 30+ günlük = 100, 0 = 20 (yeni ürün cezası düşük)
        var salesVelocity = Math.Min(100, 20 + input.SalesLast30Days * 2.67m);

        // Stok tutarlılık: %100 = 100, %50 = 0
        var stockScore = Math.Max(0, (input.StockConsistencyRate - 50m) * 2m);

        return salesVelocity * 0.60m + stockScore * 0.40m;
    }

    private static decimal CalculateLogisticsScore(ProductReliabilityInput input)
    {
        // Teslimat süresi: 1 gün = 100, 7+ gün = 0
        var deliveryScore = Math.Max(0, 100 - (input.AverageDeliveryDays - 1m) * 16.67m);

        // Hasar oranı: %0 = 100, %5+ = 0
        var damageScore = Math.Max(0, 100 - input.DamageRate * 20);

        // Zamanında teslim: %100 = 100, %70 = 0
        var onTimeScore = Math.Max(0, (input.OnTimeDeliveryRate - 70m) * 3.33m);

        return deliveryScore * 0.30m + damageScore * 0.30m + onTimeScore * 0.40m;
    }

    // DEMİR KARAR: Renk eşikleri DEĞİŞMEZ (emirname D12-24)
    private static ReliabilityColor GetColor(decimal score) => score switch
    {
        >= 90 => ReliabilityColor.Green,
        >= 70 => ReliabilityColor.Yellow,
        >= 50 => ReliabilityColor.Orange,
        _ => ReliabilityColor.Red
    };
}
