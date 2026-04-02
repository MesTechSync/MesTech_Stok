using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// eBay platform endpoint'leri — Swagger'da görünür (G10821).
/// GET  /api/v1/ebay/products    — eBay ürün listesi
/// GET  /api/v1/ebay/categories  — eBay kategori listesi
/// GET  /api/v1/ebay/connection  — eBay bağlantı testi
/// </summary>
public static class EbayEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/ebay")
            .WithTags("eBay")
            .RequireAuthorization()
            .RequireRateLimiting("PerApiKey");

        group.MapGet("/products", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("ebay");
            if (adapter is null)
                return Results.Problem(
                    detail: "eBay adapter bulunamadi — DI kaydi kontrol edin.",
                    statusCode: 503);

            var products = await adapter.PullProductsAsync(ct);
            return Results.Ok(new
            {
                platform = "eBay",
                count = products.Count,
                products
            });
        })
        .WithName("GetEbayProducts")
        .WithSummary("eBay urunlerini cek")
        .Produces(200)
        .ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/categories", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("ebay");
            if (adapter is null)
                return Results.Problem(detail: "eBay adapter bulunamadi.", statusCode: 503);

            var categories = await adapter.GetCategoriesAsync(ct);
            return Results.Ok(new
            {
                platform = "eBay",
                count = categories.Count,
                categories
            });
        })
        .WithName("GetEbayCategories")
        .WithSummary("eBay kategori listesi")
        .Produces(200)
        .ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/connection", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("ebay");
            if (adapter is null)
                return Results.Problem(detail: "eBay adapter bulunamadi.", statusCode: 503);

            var result = await adapter.TestConnectionAsync(new Dictionary<string, string>(), ct);
            return Results.Ok(new
            {
                platform = "eBay",
                isConnected = result.IsSuccess,
                storeName = result.StoreName,
                productCount = result.ProductCount,
                errorMessage = result.ErrorMessage,
                responseTimeMs = result.ResponseTime.TotalMilliseconds
            });
        })
        .WithName("TestEbayConnection")
        .WithSummary("eBay API baglanti testi")
        .Produces(200)
        .ProducesProblem(503);
    }
}
