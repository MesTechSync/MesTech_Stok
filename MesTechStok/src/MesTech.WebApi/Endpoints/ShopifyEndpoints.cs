using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Shopify platform endpoint'leri — Swagger'da görünür (G10821).
/// GET  /api/v1/shopify/products    — Shopify ürün listesi
/// GET  /api/v1/shopify/categories  — Shopify kategori listesi
/// GET  /api/v1/shopify/connection  — Shopify bağlantı testi
/// </summary>
public static class ShopifyEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/shopify")
            .WithTags("Shopify")
            .RequireAuthorization()
            .RequireRateLimiting("PerApiKey");

        group.MapGet("/products", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("shopify");
            if (adapter is null)
                return Results.Problem(
                    detail: "Shopify adapter bulunamadi — DI kaydi kontrol edin.",
                    statusCode: 503);

            var products = await adapter.PullProductsAsync(ct);
            return Results.Ok(new
            {
                platform = "Shopify",
                count = products.Count,
                products
            });
        })
        .WithName("GetShopifyProducts")
        .WithSummary("Shopify urunlerini cek")
        .Produces(200)
        .ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/categories", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("shopify");
            if (adapter is null)
                return Results.Problem(detail: "Shopify adapter bulunamadi.", statusCode: 503);

            var categories = await adapter.GetCategoriesAsync(ct);
            return Results.Ok(new
            {
                platform = "Shopify",
                count = categories.Count,
                categories
            });
        })
        .WithName("GetShopifyCategories")
        .WithSummary("Shopify kategori listesi")
        .Produces(200)
        .ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/connection", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("shopify");
            if (adapter is null)
                return Results.Problem(detail: "Shopify adapter bulunamadi.", statusCode: 503);

            var result = await adapter.TestConnectionAsync(new Dictionary<string, string>(), ct);
            return Results.Ok(new
            {
                platform = "Shopify",
                isConnected = result.IsSuccess,
                storeName = result.StoreName,
                productCount = result.ProductCount,
                errorMessage = result.ErrorMessage,
                responseTimeMs = result.ResponseTime.TotalMilliseconds
            });
        })
        .WithName("TestShopifyConnection")
        .WithSummary("Shopify API baglanti testi")
        .Produces(200)
        .ProducesProblem(503);
    }
}
