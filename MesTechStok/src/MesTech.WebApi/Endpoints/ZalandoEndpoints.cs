using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Zalando platform endpoint'leri — Swagger'da görünür (G10821).
/// GET  /api/v1/zalando/products    — Zalando ürün listesi
/// GET  /api/v1/zalando/categories  — Zalando kategori listesi
/// GET  /api/v1/zalando/connection  — Zalando bağlantı testi
/// </summary>
public static class ZalandoEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/zalando")
            .WithTags("Zalando")
            .RequireAuthorization()
            .RequireRateLimiting("PerApiKey");

        group.MapGet("/products", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("zalando");
            if (adapter is null)
                return Results.Problem(
                    detail: "Zalando adapter bulunamadi — DI kaydi kontrol edin.",
                    statusCode: 503);

            var products = await adapter.PullProductsAsync(ct);
            return Results.Ok(new
            {
                platform = "Zalando",
                count = products.Count,
                products
            });
        })
        .WithName("GetZalandoProducts")
        .WithSummary("Zalando urunlerini cek")
        .Produces(200)
        .ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/categories", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("zalando");
            if (adapter is null)
                return Results.Problem(detail: "Zalando adapter bulunamadi.", statusCode: 503);

            var categories = await adapter.GetCategoriesAsync(ct);
            return Results.Ok(new
            {
                platform = "Zalando",
                count = categories.Count,
                categories
            });
        })
        .WithName("GetZalandoCategories")
        .WithSummary("Zalando kategori listesi")
        .Produces(200)
        .ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/connection", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("zalando");
            if (adapter is null)
                return Results.Problem(detail: "Zalando adapter bulunamadi.", statusCode: 503);

            var result = await adapter.TestConnectionAsync(new Dictionary<string, string>(), ct);
            return Results.Ok(new
            {
                platform = "Zalando",
                isConnected = result.IsSuccess,
                storeName = result.StoreName,
                productCount = result.ProductCount,
                errorMessage = result.ErrorMessage,
                responseTimeMs = result.ResponseTime.TotalMilliseconds
            });
        })
        .WithName("TestZalandoConnection")
        .WithSummary("Zalando API baglanti testi")
        .Produces(200)
        .ProducesProblem(503);
    }
}
