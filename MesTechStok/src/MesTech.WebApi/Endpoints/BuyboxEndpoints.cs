using MediatR;
using MesTech.Application.Features.Product.Queries.GetBuyboxStatus;
using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Buybox analizi ve fiyat optimizasyon endpoint'leri.
/// GET  /api/v1/buybox/{productId}         — Tek ürün buybox durumu
/// GET  /api/v1/buybox/positions            — Platform bazlı buybox pozisyonları
/// GET  /api/v1/buybox/lost                 — Kaybedilen buybox'lar
/// GET  /api/v1/pricing/optimize/{productId} — Tek ürün fiyat optimizasyonu
/// GET  /api/v1/pricing/optimize/bulk       — Toplu fiyat optimizasyonu
/// GET  /api/v1/pricing/history/{productId} — Fiyat geçmişi
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
        .Produces(200);

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
        .Produces(200);

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
        .Produces(200);

        // ── Price Optimization ──

        app.MapGet("/api/v1/pricing/optimize/{productId:guid}", async (
            Guid productId,
            decimal currentPrice,
            decimal costPrice,
            string platformCode,
            IPriceOptimizationService priceService,
            CancellationToken ct) =>
        {
            var result = await priceService.OptimizePriceAsync(
                productId, currentPrice, costPrice, platformCode, ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .RequireRateLimiting("PerApiKey")
        .WithTags("Pricing")
        .WithName("OptimizePrice")
        .WithSummary("AI fiyat optimizasyonu — marj analizi, strateji önerisi")
        .Produces(200);

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
        .Produces(200);

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
        .Produces(200);
    }
}
