using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Ciceksepeti platform endpoint'leri — Swagger'da görünür (G10821).
/// GET  /api/v1/ciceksepeti/products    — Ciceksepeti ürün listesi
/// GET  /api/v1/ciceksepeti/categories  — Ciceksepeti kategori listesi
/// GET  /api/v1/ciceksepeti/connection  — Ciceksepeti bağlantı testi
/// </summary>
public static class CiceksepetiEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/ciceksepeti")
            .WithTags("Ciceksepeti")
            .RequireAuthorization()
            .RequireRateLimiting("PerApiKey");

        group.MapGet("/products", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("ciceksepeti");
            if (adapter is null)
                return Results.Problem(
                    detail: "Ciceksepeti adapter bulunamadi — DI kaydi kontrol edin.",
                    statusCode: 503);

            var products = await adapter.PullProductsAsync(ct);
            return Results.Ok(new
            {
                platform = "Ciceksepeti",
                count = products.Count,
                products
            });
        })
        .WithName("GetCiceksepetiProducts")
        .WithSummary("Ciceksepeti urunlerini cek")
        .Produces(200)
        .ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/categories", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("ciceksepeti");
            if (adapter is null)
                return Results.Problem(detail: "Ciceksepeti adapter bulunamadi.", statusCode: 503);

            var categories = await adapter.GetCategoriesAsync(ct);
            return Results.Ok(new
            {
                platform = "Ciceksepeti",
                count = categories.Count,
                categories
            });
        })
        .WithName("GetCiceksepetiCategories")
        .WithSummary("Ciceksepeti kategori listesi")
        .Produces(200)
        .ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/connection", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("ciceksepeti");
            if (adapter is null)
                return Results.Problem(detail: "Ciceksepeti adapter bulunamadi.", statusCode: 503);

            var result = await adapter.TestConnectionAsync(new Dictionary<string, string>(), ct);
            return Results.Ok(new
            {
                platform = "Ciceksepeti",
                isConnected = result.IsSuccess,
                storeName = result.StoreName,
                productCount = result.ProductCount,
                errorMessage = result.ErrorMessage,
                responseTimeMs = result.ResponseTime.TotalMilliseconds
            });
        })
        .WithName("TestCiceksepetiConnection")
        .WithSummary("Ciceksepeti API baglanti testi")
        .Produces(200)
        .ProducesProblem(503);
    }
}
