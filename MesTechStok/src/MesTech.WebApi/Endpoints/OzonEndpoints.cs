using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Ozon platform endpoint'leri — Swagger'da görünür (G10821).
/// GET  /api/v1/ozon/products    — Ozon ürün listesi
/// GET  /api/v1/ozon/categories  — Ozon kategori listesi
/// GET  /api/v1/ozon/connection  — Ozon bağlantı testi
/// </summary>
public static class OzonEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/ozon")
            .WithTags("Ozon")
            .RequireAuthorization()
            .RequireRateLimiting("PerApiKey");

        group.MapGet("/products", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("ozon");
            if (adapter is null)
                return Results.Problem(
                    detail: "Ozon adapter bulunamadi — DI kaydi kontrol edin.",
                    statusCode: 503);

            var products = await adapter.PullProductsAsync(ct);
            return Results.Ok(new
            {
                platform = "Ozon",
                count = products.Count,
                products
            });
        })
        .WithName("GetOzonProducts")
        .WithSummary("Ozon urunlerini cek")
        .Produces(200)
        .ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/categories", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("ozon");
            if (adapter is null)
                return Results.Problem(detail: "Ozon adapter bulunamadi.", statusCode: 503);

            var categories = await adapter.GetCategoriesAsync(ct);
            return Results.Ok(new
            {
                platform = "Ozon",
                count = categories.Count,
                categories
            });
        })
        .WithName("GetOzonCategories")
        .WithSummary("Ozon kategori listesi")
        .Produces(200)
        .ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/connection", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("ozon");
            if (adapter is null)
                return Results.Problem(detail: "Ozon adapter bulunamadi.", statusCode: 503);

            var result = await adapter.TestConnectionAsync(new Dictionary<string, string>(), ct);
            return Results.Ok(new
            {
                platform = "Ozon",
                isConnected = result.IsSuccess,
                storeName = result.StoreName,
                productCount = result.ProductCount,
                errorMessage = result.ErrorMessage,
                responseTimeMs = result.ResponseTime.TotalMilliseconds
            });
        })
        .WithName("TestOzonConnection")
        .WithSummary("Ozon API baglanti testi")
        .Produces(200)
        .ProducesProblem(503);
    }
}
