namespace MesTech.Application.Services;

/// <summary>
/// Calculates a reliability score (0-100) for a SupplierFeed based on 5 weighted components.
/// Pure calculation service — no I/O, no async. The caller gathers metrics data.
/// </summary>
public class FeedReliabilityScoreService
{
    private const double StockAccuracyWeight = 0.25;
    private const double UpdateFrequencyWeight = 0.20;
    private const double FeedAvailabilityWeight = 0.20;
    private const double ProductStabilityWeight = 0.20;
    private const double ResponseTimeWeight = 0.15;

    /// <summary>
    /// Calculates reliability score without associating it to a specific feed.
    /// </summary>
    public SupplierReliabilityScore Calculate(FeedReliabilityInput input)
    {
        return CalculateForFeed(Guid.Empty, input);
    }

    /// <summary>
    /// Calculates reliability score for a specific SupplierFeed.
    /// </summary>
    public SupplierReliabilityScore CalculateForFeed(Guid supplierFeedId, FeedReliabilityInput input)
    {
        var stockAccuracyScore = Clamp(input.StockAccuracyPercent);
        var updateFrequencyScore = Clamp(input.UpdateFrequencyPercent);
        var feedAvailabilityScore = Clamp(input.FeedAvailabilityPercent);
        var productStabilityScore = Clamp(input.ProductStabilityPercent);
        var responseTimeScore = CalculateResponseTimeScore(input.AverageResponseTimeMs);

        var weightedScore =
            stockAccuracyScore * StockAccuracyWeight +
            updateFrequencyScore * UpdateFrequencyWeight +
            feedAvailabilityScore * FeedAvailabilityWeight +
            productStabilityScore * ProductStabilityWeight +
            responseTimeScore * ResponseTimeWeight;

        var finalScore = (int)Math.Round(Clamp(weightedScore));
        var (color, label) = MapToColorAndLabel(finalScore);

        return new SupplierReliabilityScore(
            SupplierFeedId: supplierFeedId,
            Score: finalScore,
            Color: color,
            ColorLabel: label,
            StockAccuracyScore: Math.Round(stockAccuracyScore, 2),
            UpdateFrequencyScore: Math.Round(updateFrequencyScore, 2),
            FeedAvailabilityScore: Math.Round(feedAvailabilityScore, 2),
            ProductStabilityScore: Math.Round(productStabilityScore, 2),
            ResponseTimeScore: Math.Round(responseTimeScore, 2)
        );
    }

    /// <summary>
    /// Converts average response time in milliseconds to a 0-100 score.
    /// Lower response time = higher score.
    /// </summary>
    private static double CalculateResponseTimeScore(double responseTimeMs)
    {
        if (responseTimeMs <= 0)
            return 100;

        if (responseTimeMs <= 500)
            return 100;

        if (responseTimeMs <= 2000)
        {
            // Linear interpolation: 500ms → 100, 2000ms → 50
            return 100 - (responseTimeMs - 500) / (2000 - 500) * (100 - 50);
        }

        if (responseTimeMs <= 5000)
        {
            // Linear interpolation: 2000ms → 50, 5000ms → 20
            return 50 - (responseTimeMs - 2000) / (5000 - 2000) * (50 - 20);
        }

        // > 5000ms
        return 0;
    }

    private static (ReliabilityColor Color, string Label) MapToColorAndLabel(int score)
    {
        return score switch
        {
            >= 90 => (ReliabilityColor.Green, "Altın Tedarikçi"),
            >= 75 => (ReliabilityColor.Yellow, "Güvenilir"),
            >= 50 => (ReliabilityColor.Orange, "Dikkatli"),
            _ => (ReliabilityColor.Red, "Riskli")
        };
    }

    /// <summary>
    /// Clamps a value to the 0-100 range.
    /// </summary>
    private static double Clamp(double value)
    {
        return Math.Clamp(value, 0, 100);
    }
}

/// <summary>
/// Input metrics for feed reliability calculation.
/// All percentage values should be in 0-100 range (clamped internally).
/// </summary>
public record FeedReliabilityInput(
    double StockAccuracyPercent,
    double UpdateFrequencyPercent,
    double FeedAvailabilityPercent,
    double ProductStabilityPercent,
    double AverageResponseTimeMs
);

/// <summary>
/// Result of a feed reliability calculation with overall score, color classification,
/// and individual component scores.
/// </summary>
public record SupplierReliabilityScore(
    Guid SupplierFeedId,
    int Score,
    ReliabilityColor Color,
    string ColorLabel,
    double StockAccuracyScore,
    double UpdateFrequencyScore,
    double FeedAvailabilityScore,
    double ProductStabilityScore,
    double ResponseTimeScore
);

/// <summary>
/// Visual reliability classification for supplier feeds.
/// </summary>
public enum ReliabilityColor
{
    /// <summary>90-100: Altın Tedarikçi (Gold Supplier)</summary>
    Green,

    /// <summary>75-89: Güvenilir (Reliable)</summary>
    Yellow,

    /// <summary>50-74: Dikkatli (Caution)</summary>
    Orange,

    /// <summary>0-49: Riskli (Risky)</summary>
    Red
}
