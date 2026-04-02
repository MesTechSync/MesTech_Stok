using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Trendyol platform endpoint'leri — Swagger'da görünür (G808-DEV6).
/// GET  /api/v1/trendyol/products    — Trendyol ürün listesi
/// GET  /api/v1/trendyol/categories  — Trendyol kategori listesi
/// GET  /api/v1/trendyol/connection  — Trendyol bağlantı testi
/// POST /api/v1/trendyol/sync        — Trendyol senkronizasyon tetikle
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
        .CacheOutput("Lookup60s");

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
    }
}
