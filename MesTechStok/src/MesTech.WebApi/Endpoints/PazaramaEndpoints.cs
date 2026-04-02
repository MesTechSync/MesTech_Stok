using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Pazarama platform endpoint'leri — Swagger'da görünür (G10821).
/// GET  /api/v1/pazarama/products    — Pazarama ürün listesi
/// GET  /api/v1/pazarama/categories  — Pazarama kategori listesi
/// GET  /api/v1/pazarama/connection  — Pazarama bağlantı testi
/// </summary>
public static class PazaramaEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/pazarama")
            .WithTags("Pazarama")
            .RequireAuthorization()
            .RequireRateLimiting("PerApiKey");

        group.MapGet("/products", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("pazarama");
            if (adapter is null)
                return Results.Problem(
                    detail: "Pazarama adapter bulunamadi — DI kaydi kontrol edin.",
                    statusCode: 503);

            var products = await adapter.PullProductsAsync(ct);
            return Results.Ok(new
            {
                platform = "Pazarama",
                count = products.Count,
                products
            });
        })
        .WithName("GetPazaramaProducts")
        .WithSummary("Pazarama urunlerini cek")
        .Produces(200)
        .ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/categories", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("pazarama");
            if (adapter is null)
                return Results.Problem(detail: "Pazarama adapter bulunamadi.", statusCode: 503);

            var categories = await adapter.GetCategoriesAsync(ct);
            return Results.Ok(new
            {
                platform = "Pazarama",
                count = categories.Count,
                categories
            });
        })
        .WithName("GetPazaramaCategories")
        .WithSummary("Pazarama kategori listesi")
        .Produces(200)
        .ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/connection", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("pazarama");
            if (adapter is null)
                return Results.Problem(detail: "Pazarama adapter bulunamadi.", statusCode: 503);

            var result = await adapter.TestConnectionAsync(new Dictionary<string, string>(), ct);
            return Results.Ok(new
            {
                platform = "Pazarama",
                isConnected = result.IsSuccess,
                storeName = result.StoreName,
                productCount = result.ProductCount,
                errorMessage = result.ErrorMessage,
                responseTimeMs = result.ResponseTime.TotalMilliseconds
            });
        })
        .WithName("TestPazaramaConnection")
        .WithSummary("Pazarama API baglanti testi")
        .Produces(200)
        .ProducesProblem(503);
    }
}
