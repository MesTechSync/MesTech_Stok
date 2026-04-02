using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Etsy platform endpoint'leri — Swagger'da görünür (G10821).
/// GET  /api/v1/etsy/products    — Etsy ürün listesi
/// GET  /api/v1/etsy/categories  — Etsy kategori listesi
/// GET  /api/v1/etsy/connection  — Etsy bağlantı testi
/// </summary>
public static class EtsyEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/etsy")
            .WithTags("Etsy")
            .RequireAuthorization()
            .RequireRateLimiting("PerApiKey");

        group.MapGet("/products", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("etsy");
            if (adapter is null)
                return Results.Problem(
                    detail: "Etsy adapter bulunamadi — DI kaydi kontrol edin.",
                    statusCode: 503);

            var products = await adapter.PullProductsAsync(ct);
            return Results.Ok(new
            {
                platform = "Etsy",
                count = products.Count,
                products
            });
        })
        .WithName("GetEtsyProducts")
        .WithSummary("Etsy urunlerini cek")
        .Produces(200)
        .ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/categories", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("etsy");
            if (adapter is null)
                return Results.Problem(detail: "Etsy adapter bulunamadi.", statusCode: 503);

            var categories = await adapter.GetCategoriesAsync(ct);
            return Results.Ok(new
            {
                platform = "Etsy",
                count = categories.Count,
                categories
            });
        })
        .WithName("GetEtsyCategories")
        .WithSummary("Etsy kategori listesi")
        .Produces(200)
        .ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/connection", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("etsy");
            if (adapter is null)
                return Results.Problem(detail: "Etsy adapter bulunamadi.", statusCode: 503);

            var result = await adapter.TestConnectionAsync(new Dictionary<string, string>(), ct);
            return Results.Ok(new
            {
                platform = "Etsy",
                isConnected = result.IsSuccess,
                storeName = result.StoreName,
                productCount = result.ProductCount,
                errorMessage = result.ErrorMessage,
                responseTimeMs = result.ResponseTime.TotalMilliseconds
            });
        })
        .WithName("TestEtsyConnection")
        .WithSummary("Etsy API baglanti testi")
        .Produces(200)
        .ProducesProblem(503);
    }
}
