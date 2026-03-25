using MesTech.Application.Interfaces;
using MesTech.Application.Services;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using AppInterfaces = MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// IFeedReliabilityScoreService adaptörü.
/// Application.Services.FeedReliabilityScoreService (static/pure) ile
/// Application.Interfaces.IFeedReliabilityScoreService (async/injectable) arasındaki köprü.
/// Veri: SupplierFeed entity'sindeki LastSync* alanlarından hesaplanır.
/// Sprint-D DEV1 — Dalga 8 Dropshipping aktivasyonu.
/// </summary>
public sealed class FeedReliabilityScoreServiceAdapter(
    AppDbContext db
) : IFeedReliabilityScoreService
{
    public async Task<AppInterfaces.SupplierReliabilityScore> CalculateAsync(
        Guid supplierFeedId, CancellationToken ct = default)
    {
        var feed = await db.SupplierFeeds
            .FirstOrDefaultAsync(f => f.Id == supplierFeedId && !f.IsDeleted, ct);

        if (feed is null)
            return null!;

        // Girdi metrikleri feed entity'den türet
        double stockAccuracy = feed.LastSyncProductCount > 0
            ? (double)feed.LastSyncUpdatedCount / feed.LastSyncProductCount * 100.0
            : 0.0;

        bool syncedInLast24h = feed.LastSyncAt.HasValue
            && (DateTime.UtcNow - feed.LastSyncAt.Value).TotalHours <= 24;
        double updateFrequency = syncedInLast24h ? 100.0 : 0.0;

        double feedAvailability = feed.LastSyncError == null ? 100.0 : 60.0;

        const double productStability = 80.0;

        double avgResponseMs = EstimateResponseTimeMs(feed);

        var input = new FeedReliabilityInput(
            StockAccuracyPercent: stockAccuracy,
            UpdateFrequencyPercent: updateFrequency,
            FeedAvailabilityPercent: feedAvailability,
            ProductStabilityPercent: productStability,
            AverageResponseTimeMs: avgResponseMs);

        // Application.Services'in static hesaplayıcısını kullan
        var serviceResult = FeedReliabilityScoreService.CalculateForFeed(supplierFeedId, input);

        // Application.Services.SupplierReliabilityScore → Application.Interfaces.SupplierReliabilityScore
        // İki record farklı namespace'de; interface'in beklediği decimal türüne dönüştür.
        var color = MapColor(serviceResult.Color);

        return new AppInterfaces.SupplierReliabilityScore(
            SupplierFeedId: serviceResult.SupplierFeedId,
            Score: serviceResult.Score,
            Color: color,
            StockAccuracy: (decimal)serviceResult.StockAccuracyScore,
            UpdateFrequency: (decimal)serviceResult.UpdateFrequencyScore,
            FeedAvailability: (decimal)serviceResult.FeedAvailabilityScore,
            ProductStability: (decimal)serviceResult.ProductStabilityScore,
            ResponseTime: (decimal)serviceResult.ResponseTimeScore);
    }

    private static AppInterfaces.ReliabilityColor MapColor(
        Application.Services.ReliabilityColor svcColor) =>
        svcColor switch
        {
            Application.Services.ReliabilityColor.Green => AppInterfaces.ReliabilityColor.Green,
            Application.Services.ReliabilityColor.Yellow => AppInterfaces.ReliabilityColor.Yellow,
            Application.Services.ReliabilityColor.Orange => AppInterfaces.ReliabilityColor.Orange,
            _ => AppInterfaces.ReliabilityColor.Red
        };

    private static double EstimateResponseTimeMs(Domain.Entities.SupplierFeed feed)
    {
        if (!feed.LastSyncAt.HasValue)
            return 1000.0;

        var delta = feed.LastSyncAt.Value - feed.UpdatedAt;
        if (delta.TotalMilliseconds > 0)
            return delta.TotalMilliseconds;

        return feed.LastSyncProductCount > 0
            ? feed.LastSyncProductCount * 5.0
            : 1000.0;
    }
}
