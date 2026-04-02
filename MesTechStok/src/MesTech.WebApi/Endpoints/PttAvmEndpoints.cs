using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// PttAVM platform endpoint'leri — Swagger'da görünür (G10821).
/// GET  /api/v1/pttavm/products    — PttAVM ürün listesi
/// GET  /api/v1/pttavm/categories  — PttAVM kategori listesi
/// GET  /api/v1/pttavm/connection  — PttAVM bağlantı testi
/// </summary>
public static class PttAvmEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/pttavm")
            .WithTags("PttAVM")
            .RequireAuthorization()
            .RequireRateLimiting("PerApiKey");

        group.MapGet("/products", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("pttavm");
            if (adapter is null)
                return Results.Problem(
                    detail: "PttAVM adapter bulunamadi — DI kaydi kontrol edin.",
                    statusCode: 503);

            var products = await adapter.PullProductsAsync(ct);
            return Results.Ok(new
            {
                platform = "PttAVM",
                count = products.Count,
                products
            });
        })
        .WithName("GetPttAvmProducts")
        .WithSummary("PttAVM urunlerini cek")
        .Produces(200)
        .ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/categories", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("pttavm");
            if (adapter is null)
                return Results.Problem(detail: "PttAVM adapter bulunamadi.", statusCode: 503);

            var categories = await adapter.GetCategoriesAsync(ct);
            return Results.Ok(new
            {
                platform = "PttAVM",
                count = categories.Count,
                categories
            });
        })
        .WithName("GetPttAvmCategories")
        .WithSummary("PttAVM kategori listesi")
        .Produces(200)
        .ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/connection", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("pttavm");
            if (adapter is null)
                return Results.Problem(detail: "PttAVM adapter bulunamadi.", statusCode: 503);

            var result = await adapter.TestConnectionAsync(new Dictionary<string, string>(), ct);
            return Results.Ok(new
            {
                platform = "PttAVM",
                isConnected = result.IsSuccess,
                storeName = result.StoreName,
                productCount = result.ProductCount,
                errorMessage = result.ErrorMessage,
                responseTimeMs = result.ResponseTime.TotalMilliseconds
            });
        })
        .WithName("TestPttAvmConnection")
        .WithSummary("PttAVM API baglanti testi")
        .Produces(200)
        .ProducesProblem(503);
    }
}
