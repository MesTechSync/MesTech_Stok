using Hangfire;
using MesTech.Application.Services;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Tüm aktif SupplierFeed'lerin güvenilirlik skorunu yeniden hesaplar,
/// ilişkili DropshippingPoolProduct kayıtlarını günceller.
/// Hangfire: her gece 03:00, concurrent çalışma yasak, 1 retry.
/// ENT-DROP-IMP-SPRINT-B — DEV 3 Görev B
/// </summary>
[DisableConcurrentExecution(timeoutInSeconds: 300)]
[AutomaticRetry(Attempts = 1)]
public class ReliabilityScoreRecalcJob
{
    private const int BatchSize = 100;

    private readonly AppDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReliabilityScoreRecalcJob> _logger;

    public ReliabilityScoreRecalcJob(
        AppDbContext dbContext,
        IUnitOfWork unitOfWork,
        ILogger<ReliabilityScoreRecalcJob> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[ReliabilityScoreRecalc] Başlatıldı.");

        // 1. Aktif feedleri al
        var activeFeeds = await _dbContext.SupplierFeeds
            .Where(f => f.IsActive && !f.IsDeleted)
            .ToListAsync(ct).ConfigureAwait(false);

        if (activeFeeds.Count == 0)
        {
            _logger.LogInformation("[ReliabilityScoreRecalc] Aktif feed bulunamadı. İş tamamlandı.");
            return;
        }

        _logger.LogInformation(
            "[ReliabilityScoreRecalc] {Count} aktif feed işlenecek.", activeFeeds.Count);

        int processed = 0;
        int updatedProducts = 0;

        // 2. Her feed için güvenilirlik girdisi hesapla ve skoru bul
        foreach (var feed in activeFeeds)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var input = BuildReliabilityInput(feed);
                var score = FeedReliabilityScoreService.Calculate(input);

                _logger.LogDebug(
                    "[ReliabilityScoreRecalc] Feed {FeedId} ({FeedName}) → Skor: {Score} ({Color})",
                    feed.Id, feed.Name, score.Score, score.Color);

                // 3. İlişkili aktif PoolProduct'ları güncelle — 100'lük batch
                var feedId = feed.Id;
                var poolProducts = await _dbContext.DropshippingPoolProducts
                    .Where(pp => pp.AddedFromFeedId == feedId && pp.IsActive && !pp.IsDeleted)
                    .ToListAsync(ct).ConfigureAwait(false);

                if (poolProducts.Count > 0)
                {
                    // Batch kaydet (100'lük gruplar)
                    foreach (var batch in poolProducts.Chunk(BatchSize))
                    {
                        ct.ThrowIfCancellationRequested();

                        // Skor düşükse (< 50 = Riskli) ürünü pasife al
                        foreach (var pp in batch)
                        {
                            if (score.Score < 50 && pp.IsActive)
                            {
                                pp.Deactivate();
                                updatedProducts++;
                            }
                            else if (score.Score >= 50 && !pp.IsActive)
                            {
                                pp.Activate();
                                updatedProducts++;
                            }
                        }

                        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    }
                }

                processed++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[ReliabilityScoreRecalc] Feed {FeedId} ({FeedName}) işlenirken hata.",
                    feed.Id, feed.Name);
                // Diğer feedlere devam et
            }
        }

        _logger.LogInformation(
            "[ReliabilityScoreRecalc] Tamamlandı. {Processed}/{Total} feed işlendi, {Updated} ürün güncellendi.",
            processed, activeFeeds.Count, updatedProducts);
    }

    /// <summary>
    /// SupplierFeed alanlarından FeedReliabilityInput hesapla.
    /// </summary>
    private static FeedReliabilityInput BuildReliabilityInput(SupplierFeed feed)
    {
        // StockAccuracyPercent: LastSyncUpdatedCount / LastSyncProductCount * 100
        double stockAccuracy = feed.LastSyncProductCount > 0
            ? (double)feed.LastSyncUpdatedCount / feed.LastSyncProductCount * 100.0
            : 0.0;

        // UpdateFrequencyPercent: son 24 saatte beklenen sync sayısına karşı gerçekleşen
        // Beklenen = 24 * 60 / SyncIntervalMinutes
        double expectedSyncsPerDay = feed.SyncIntervalMinutes > 0
            ? 24.0 * 60.0 / feed.SyncIntervalMinutes
            : 1.0;
        // Son sync varsa ve 24 saatten yeni ise tam puan, yoksa 0
        bool syncedInLast24h = feed.LastSyncAt.HasValue
            && (DateTime.UtcNow - feed.LastSyncAt.Value).TotalHours <= 24;
        double updateFrequency = syncedInLast24h ? 100.0 : 0.0;

        // FeedAvailabilityPercent: son sync başarılı mı?
        // LastSyncError null ise başarılı, varsa hatalı
        // Basit hesap: hata yoksa 100, hata varsa 60 (kısmi puan)
        double feedAvailability = feed.LastSyncError == null
            ? 100.0
            : 60.0;

        // ProductStabilityPercent: sabit 80.0 (gelecekte gerçek hesaplama yapılacak)
        const double productStability = 80.0;

        // AverageResponseTimeMs: son sync süresi tahmini
        // LastSyncAt - UpdatedAt delta milisaniye cinsinden (yaklaşık)
        double avgResponseMs = EstimateResponseTimeMs(feed);

        return new FeedReliabilityInput(
            StockAccuracyPercent: stockAccuracy,
            UpdateFrequencyPercent: updateFrequency,
            FeedAvailabilityPercent: feedAvailability,
            ProductStabilityPercent: productStability,
            AverageResponseTimeMs: avgResponseMs);
    }

    /// <summary>
    /// Feed sync süresini tahmini olarak hesaplar.
    /// LastSyncAt ile UpdatedAt delta kullanılır. Veri yoksa 1000ms varsayılır.
    /// </summary>
    private static double EstimateResponseTimeMs(SupplierFeed feed)
    {
        if (!feed.LastSyncAt.HasValue)
            return 1000.0;

        var delta = feed.LastSyncAt.Value - feed.UpdatedAt;
        if (delta.TotalMilliseconds > 0)
            return delta.TotalMilliseconds;

        // Ürün sayısına göre kaba tahmin: her ürün ~5ms
        return feed.LastSyncProductCount > 0
            ? feed.LastSyncProductCount * 5.0
            : 1000.0;
    }
}
