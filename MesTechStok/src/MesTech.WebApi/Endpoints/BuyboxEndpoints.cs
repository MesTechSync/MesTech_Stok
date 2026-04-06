using Hangfire;
using Hangfire.Storage;
using MediatR;
using MesTech.Application.Features.Product.Queries.GetBuyboxStatus;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Jobs.Pricing;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Buybox analizi ve fiyat optimizasyon endpoint'leri.
/// GET  /api/v1/buybox/{productId}         — Tek ürün buybox durumu
/// GET  /api/v1/buybox/positions            — Platform bazlı buybox pozisyonları
/// GET  /api/v1/buybox/lost                 — Kaybedilen buybox'lar
/// GET  /api/v1/pricing/optimize/{productId} — Tek ürün fiyat optimizasyonu
/// GET  /api/v1/pricing/optimize/bulk       — Toplu fiyat optimizasyonu
/// GET  /api/v1/pricing/history/{productId} — Fiyat geçmişi
/// GET  /api/v1/pricing/dashboard           — Pricing intelligence dashboard (DEV6-F)
/// GET  /api/v1/pricing/auto-config         — Auto-price schedule config (DEV6-F)
/// PUT  /api/v1/pricing/auto-config         — Update auto-price schedule (DEV6-F)
/// POST /api/v1/pricing/auto-trigger        — Manual trigger auto-price cycle (DEV6-F)
/// </summary>
public static class BuyboxEndpoints
{
    public static void Map(WebApplication app)
    {
        // ── Buybox ──

        app.MapGet("/api/v1/buybox/{productId:guid}", async (
            Guid productId,
            Guid tenantId,
            string? platformCode,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetBuyboxStatusQuery(tenantId, productId, platformCode), ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .RequireRateLimiting("PerApiKey")
        .WithTags("Buybox")
        .WithName("GetBuyboxStatus")
        .WithSummary("Tek ürün buybox durumu — rakip fiyat, pozisyon, öneri")
        .Produces<BuyboxStatusResult>(200);

        app.MapGet("/api/v1/buybox/positions", async (
            Guid tenantId,
            string platformCode,
            IBuyboxService buyboxService,
            CancellationToken ct) =>
        {
            var positions = await buyboxService.CheckBuyboxPositionsAsync(
                tenantId, platformCode, ct);
            return Results.Ok(positions);
        })
        .RequireAuthorization()
        .RequireRateLimiting("PerApiKey")
        .WithTags("Buybox")
        .WithName("GetBuyboxPositions")
        .WithSummary("Platform bazlı tüm ürünlerin buybox pozisyonları")
        .Produces<IReadOnlyList<BuyboxPosition>>(200);

        app.MapGet("/api/v1/buybox/analyze", async (
            string sku,
            decimal currentPrice,
            string platformCode,
            int? minSellerRating,
            IBuyboxService buyboxService,
            CancellationToken ct) =>
        {
            var result = minSellerRating.HasValue
                ? await buyboxService.AnalyzeCompetitorsAsync(
                    sku, currentPrice, platformCode, minSellerRating.Value, ct)
                : await buyboxService.AnalyzeCompetitorsAsync(
                    sku, currentPrice, platformCode, ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .RequireRateLimiting("PerApiKey")
        .WithTags("Buybox")
        .WithName("AnalyzeCompetitors")
        .WithSummary("Rakip analizi — opsiyonel minSellerRating ile düşük puanlı satıcıları filtrele")
        .Produces<BuyboxAnalysis>(200);

        app.MapGet("/api/v1/buybox/lost", async (
            Guid tenantId,
            IBuyboxService buyboxService,
            CancellationToken ct) =>
        {
            var lost = await buyboxService.GetLostBuyboxesAsync(tenantId, ct);
            return Results.Ok(lost);
        })
        .RequireAuthorization()
        .RequireRateLimiting("PerApiKey")
        .WithTags("Buybox")
        .WithName("GetLostBuyboxes")
        .WithSummary("Son kaybedilen buybox'lar — fiyat düşürme fırsatları")
        .Produces<IReadOnlyList<BuyboxLostItem>>(200);

        // ── Price Optimization ──

        app.MapGet("/api/v1/pricing/optimize/{productId:guid}", async (
            Guid productId,
            decimal currentPrice,
            decimal costPrice,
            string platformCode,
            IPriceOptimizationService priceService,
            CancellationToken ct) =>
        {
            if (currentPrice <= 0 || costPrice < 0)
                return Results.Problem(detail: "currentPrice must be > 0 and costPrice must be >= 0.", statusCode: 400);
            var result = await priceService.OptimizePriceAsync(
                productId, currentPrice, costPrice, platformCode, ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .RequireRateLimiting("PerApiKey")
        .WithTags("Pricing")
        .WithName("OptimizePrice")
        .WithSummary("AI fiyat optimizasyonu — marj analizi, strateji önerisi")
        .Produces<PriceOptimization>(200);

        app.MapGet("/api/v1/pricing/optimize/bulk", async (
            Guid tenantId,
            string? platformCode,
            string? categoryId,
            IPriceOptimizationService priceService,
            CancellationToken ct) =>
        {
            var results = await priceService.OptimizeBulkAsync(
                tenantId, platformCode, categoryId, ct);
            return Results.Ok(results);
        })
        .RequireAuthorization()
        .RequireRateLimiting("PerApiKey")
        .WithTags("Pricing")
        .WithName("OptimizePriceBulk")
        .WithSummary("Toplu fiyat optimizasyonu — tüm ürünler veya kategori/platform bazlı")
        .Produces<IReadOnlyList<PriceOptimization>>(200);

        app.MapGet("/api/v1/pricing/history/{productId:guid}", async (
            Guid productId,
            int? days,
            IPriceOptimizationService priceService,
            CancellationToken ct) =>
        {
            var history = await priceService.GetPriceHistoryAsync(
                productId, days ?? 30, ct);
            return Results.Ok(history);
        })
        .RequireAuthorization()
        .RequireRateLimiting("PerApiKey")
        .WithTags("Pricing")
        .WithName("GetPriceHistory")
        .WithSummary("Fiyat geçmişi — platform fiyat + AI önerisi zaman serisi")
        .Produces<PriceHistory>(200);

        // ── Pricing Intelligence Dashboard (DEV6-F) ──

        app.MapGet("/api/v1/pricing/dashboard", async (
            Guid tenantId,
            IBuyboxService buyboxService,
            IPriceOptimizationService priceService,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            // Sequential — Task.WhenAll causes concurrent DbContext access (G67/KÖK-1)
            var lost = await buyboxService.GetLostBuyboxesAsync(tenantId, ct);
            var optimizations = await priceService.OptimizeBulkAsync(tenantId, ct: ct);

            RecurringJobDto? jobInfo = null;
            try
            {
                using var connection = JobStorage.Current.GetConnection();
                jobInfo = connection.GetRecurringJobs()
                    .FirstOrDefault(j => j.Id == AutoPriceUpdateWorker.JobId);
            }
            catch (Exception ex)
            {
                loggerFactory.CreateLogger("BuyboxEndpoints")
                    .LogWarning(ex, "[PricingDashboard] Hangfire storage unavailable — auto-price status skipped");
            }

            return Results.Ok(new PricingDashboardResponse(
                lost.Count,
                lost.Take(10).ToList(),
                optimizations.Count,
                optimizations
                    .Where(o => o.CurrentPrice > 0 && Math.Abs(o.RecommendedPrice - o.CurrentPrice) / o.CurrentPrice > 0.01m)
                    .OrderByDescending(o => Math.Abs(o.RecommendedPrice - o.CurrentPrice))
                    .Take(20).ToList(),
                new AutoPriceStatusResponse(
                    jobInfo is not null,
                    AutoPriceUpdateWorker.CronExpression,
                    jobInfo?.LastExecution?.ToString("o"),
                    jobInfo?.NextExecution?.ToString("o"),
                    jobInfo?.LastJobState)));
        })
        .RequireAuthorization()
        .RequireRateLimiting("PerApiKey")
        .WithTags("Pricing")
        .WithName("GetPricingDashboard")
        .WithSummary("Pricing intelligence dashboard — lost buybox + suggestions + auto-price status")
        .Produces<PricingDashboardResponse>(200)
        .CacheOutput("Report120s");

        // ── Auto-Price Configuration (DEV6-F) ──

        app.MapGet("/api/v1/pricing/auto-config", (ILoggerFactory loggerFactory, CancellationToken _) =>
        {
            RecurringJobDto? jobInfo = null;
            try
            {
                using var connection = JobStorage.Current.GetConnection();
                jobInfo = connection.GetRecurringJobs()
                    .FirstOrDefault(j => j.Id == AutoPriceUpdateWorker.JobId);
            }
            catch (Exception ex)
            {
                loggerFactory.CreateLogger("BuyboxEndpoints")
                    .LogWarning(ex, "[AutoConfig] Hangfire storage unavailable — returning defaults");
            }

            return Results.Ok(new AutoPriceConfigResponse(
                AutoPriceUpdateWorker.JobId,
                AutoPriceUpdateWorker.CronExpression,
                jobInfo is not null,
                jobInfo?.LastExecution?.ToString("o"),
                jobInfo?.NextExecution?.ToString("o"),
                jobInfo?.LastJobState,
                2,
                "Buybox recovery — her 30 dakikada kayıp buybox taraması + otomatik fiyat güncelleme"));
        })
        .RequireAuthorization()
        .RequireRateLimiting("PerApiKey")
        .WithTags("Pricing")
        .WithName("GetAutoPriceConfig")
        .WithSummary("Auto-price schedule configuration — cron, son çalışma, durum");

        app.MapPost("/api/v1/pricing/auto-trigger", (
            ILoggerFactory loggerFactory,
            CancellationToken _) =>
        {
            var logger = loggerFactory.CreateLogger("BuyboxEndpoints");
            try
            {
                var jobId = BackgroundJob.Enqueue<AutoPriceUpdateWorker>(
                    w => w.ExecuteAsync(CancellationToken.None));

                logger.LogInformation("[AutoPrice] Manual trigger dispatched — jobId={JobId}", jobId);

                return Results.Ok(new AutoPriceTriggerResponse(
                    true, jobId,
                    "Auto-price cycle enqueued — check /api/v1/pricing/auto-config for status"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[AutoPrice] Manual trigger failed");
                return Results.Problem(
                    detail: "Auto-price trigger failed. Hangfire may not be configured.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .RequireAuthorization()
        .RequireRateLimiting("PerApiKey")
        .WithTags("Pricing")
        .WithName("TriggerAutoPrice")
        .WithSummary("Manuel auto-price tetikle — zamanlanmış döngüyü hemen çalıştır")
        .Produces(200)
        .Produces(503)
        .AddEndpointFilter<Filters.IdempotencyFilter>();
    }

    // ── Typed Response DTOs — Swagger contract stability (G538) ──
    public sealed record PricingDashboardResponse(
        int LostBuyboxCount, IReadOnlyList<BuyboxLostItem> LostBuyboxes,
        int OptimizationCount, IReadOnlyList<PriceOptimization> PriceChangeSuggestions,
        AutoPriceStatusResponse AutoPrice);
    public sealed record AutoPriceStatusResponse(
        bool IsEnabled, string CronExpression,
        string? LastExecution, string? NextExecution, string? LastJobState);
    public sealed record AutoPriceConfigResponse(
        string JobId, string CronExpression, bool IsRegistered,
        string? LastExecution, string? NextExecution, string? LastJobState,
        int RetryAttempts, string Description);
    public sealed record AutoPriceTriggerResponse(
        bool Triggered, string HangfireJobId, string Message);
}
