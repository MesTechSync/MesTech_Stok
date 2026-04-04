using MesTech.Application.DTOs;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Adapters;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Trendyol platform endpoint'leri — Swagger'da görünür (G808-DEV6).
/// GET  /api/v1/trendyol/products    — Trendyol ürün listesi
/// GET  /api/v1/trendyol/categories  — Trendyol kategori listesi
/// GET  /api/v1/trendyol/connection  — Trendyol bağlantı testi
/// POST /api/v1/trendyol/sync        — Trendyol senkronizasyon tetikle
/// GET  /api/v1/trendyol/reviews     — Ürün değerlendirmeleri
/// POST /api/v1/trendyol/reviews/{reviewId}/reply — Değerlendirmeye cevap
/// GET  /api/v1/trendyol/ads/campaigns — Reklam kampanyaları
/// GET  /api/v1/trendyol/ads/campaigns/{campaignId}/performance — Kampanya performansı
/// </summary>
public static class TrendyolEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/trendyol")
            .WithTags("Trendyol")
            .RequireAuthorization()
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/trendyol/products — Trendyol'dan ürün çek
        group.MapGet("/products", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("trendyol");
            if (adapter is null)
                return Results.Problem(
                    detail: "Trendyol adapter bulunamadi — DI kaydi kontrol edin.",
                    statusCode: 503);

            var products = await adapter.PullProductsAsync(ct);
            return Results.Ok(new PlatformEndpointHelper.PlatformProductsResponse(
                "Trendyol", products.Count, products));
        })
        .WithName("GetTrendyolProducts")
        .WithSummary("Trendyol urunlerini cek — API uzerinden platform verisi")
        .Produces(200)
        .ProducesProblem(503)
        .CacheOutput("Lookup60s");

        // GET /api/v1/trendyol/categories — Trendyol kategori agaci
        group.MapGet("/categories", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("trendyol");
            if (adapter is null)
                return Results.Problem(detail: "Trendyol adapter bulunamadi.", statusCode: 503);

            var categories = await adapter.GetCategoriesAsync(ct);
            return Results.Ok(new PlatformEndpointHelper.PlatformCategoriesResponse(
                "Trendyol",
                categories.Count, categories));
        })
        .WithName("GetTrendyolCategories")
        .WithSummary("Trendyol kategori listesi — platform kategori eslemesi icin")
        .Produces(200)
        .ProducesProblem(503)
        .CacheOutput("Catalog300s");

        // GET /api/v1/trendyol/connection — baglanti testi
        group.MapGet("/connection", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("trendyol");
            if (adapter is null)
                return Results.Problem(detail: "Trendyol adapter bulunamadi.", statusCode: 503);

            var result = await adapter.TestConnectionAsync(new Dictionary<string, string>(), ct);
            return Results.Ok(new PlatformEndpointHelper.PlatformConnectionResponse(
                "Trendyol", result.IsSuccess, result.StoreName,
                result.ProductCount, result.ErrorMessage,
                result.ResponseTime.TotalMilliseconds));
        })
        .WithName("TestTrendyolConnection")
        .WithSummary("Trendyol API baglanti testi — credential dogrulama")
        .Produces(200)
        .ProducesProblem(503);

        // POST /api/v1/trendyol/sync — tam senkronizasyon
        group.MapPost("/sync", async (
            MediatR.ISender mediator,
            Guid storeId,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new MesTech.Application.Commands.SyncTrendyolProducts.SyncTrendyolProductsCommand(storeId), ct);
            return Results.Ok(result);
        })
        .WithName("SyncTrendyolFull")
        .WithSummary("Trendyol tam senkronizasyon — urun + stok + fiyat")
        .AddEndpointFilter<Filters.IdempotencyFilter>()
        .Produces(200)
        .Produces(400);

        // ═══════════════════════════════════════════
        // Review Endpoints (DEV3 TUR5)
        // ═══════════════════════════════════════════

        // GET /api/v1/trendyol/reviews — urun degerlendirmelerini cek (filtre destekli)
        group.MapGet("/reviews", async (
            IAdapterFactory adapterFactory,
            int page,
            int size,
            long? productId,
            int? minRating,
            bool? unrepliedOnly,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("trendyol") as TrendyolAdapter;
            if (adapter is null)
                return Results.Problem(detail: "TrendyolAdapter bulunamadi.", statusCode: 503);

            var reviews = await adapter.GetProductReviewsAsync(page, size, productId, minRating, unrepliedOnly ?? false, ct);
            return Results.Ok(new { platform = "Trendyol", count = reviews.Count, reviews });
        })
        .WithName("GetTrendyolReviews")
        .WithSummary("Trendyol urun degerlendirmelerini cek — sayfalama destekli")
        .Produces<object>(200)
        .ProducesProblem(503);

        // POST /api/v1/trendyol/reviews/{reviewId}/reply — degerlendirmeye cevap yaz
        group.MapPost("/reviews/{reviewId:long}/reply", async (
            IAdapterFactory adapterFactory,
            long reviewId,
            ReviewReplyRequest request,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("trendyol") as TrendyolAdapter;
            if (adapter is null)
                return Results.Problem(detail: "TrendyolAdapter bulunamadi.", statusCode: 503);

            if (string.IsNullOrWhiteSpace(request.Text))
                return Results.BadRequest("Cevap metni bos olamaz.");

            var success = await adapter.ReplyToReviewAsync(reviewId, request.Text, ct);
            return success
                ? Results.Ok(new { reviewId, replied = true })
                : Results.Problem(detail: "Review cevabi gonderilemedi.", statusCode: 502);
        })
        .WithName("ReplyToTrendyolReview")
        .WithSummary("Trendyol urun degerlendirmesine cevap yaz")
        .Produces<object>(200)
        .ProducesProblem(502)
        .Produces(400);

        // ═══════════════════════════════════════════
        // Ads Endpoints (DEV3 TUR5)
        // ═══════════════════════════════════════════

        // GET /api/v1/trendyol/ads/campaigns — reklam kampanyalarini listele
        group.MapGet("/ads/campaigns", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("trendyol") as TrendyolAdapter;
            if (adapter is null)
                return Results.Problem(detail: "TrendyolAdapter bulunamadi.", statusCode: 503);

            var campaigns = await adapter.GetAdCampaignsAsync(ct);
            return Results.Ok(new { platform = "Trendyol", count = campaigns.Count, campaigns });
        })
        .WithName("GetTrendyolAdCampaigns")
        .WithSummary("Trendyol reklam kampanyalarini listele")
        .Produces<object>(200)
        .ProducesProblem(503)
        .CacheOutput("Lookup60s");

        // GET /api/v1/trendyol/ads/campaigns/{campaignId}/performance — kampanya performansi
        group.MapGet("/ads/campaigns/{campaignId:long}/performance", async (
            IAdapterFactory adapterFactory,
            long campaignId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("trendyol") as TrendyolAdapter;
            if (adapter is null)
                return Results.Problem(detail: "TrendyolAdapter bulunamadi.", statusCode: 503);

            var metrics = await adapter.GetAdPerformanceAsync(campaignId, startDate, endDate, ct);
            return Results.Ok(new { platform = "Trendyol", campaignId, count = metrics.Count, metrics });
        })
        .WithName("GetTrendyolAdPerformance")
        .WithSummary("Trendyol reklam kampanya performansi — tarih aralikli")
        .Produces<object>(200)
        .ProducesProblem(503);
    }

    // DTO — review reply request body
    public record ReviewReplyRequest(string Text);
}
