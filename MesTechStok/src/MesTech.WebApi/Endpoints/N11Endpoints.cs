using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// N11 platform endpoint'leri — Swagger tag: "N11" (G10821-DEV6).
/// </summary>
public static class N11Endpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/n11")
            .WithTags("N11")
            .RequireAuthorization()
            .RequireRateLimiting("PerApiKey");

        group.MapGet("/products", async (IAdapterFactory f, CancellationToken ct) =>
        {
            var adapter = f.Resolve("n11");
            if (adapter is null) return Results.Problem(detail: "N11 adapter bulunamadi.", statusCode: 503);
            var products = await adapter.PullProductsAsync(ct);
            return Results.Ok(new { platform = "N11", count = products.Count, products });
        })
        .WithName("GetN11Products")
        .WithSummary("N11 urunlerini cek")
        .Produces(200).ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/categories", async (IAdapterFactory f, CancellationToken ct) =>
        {
            var adapter = f.Resolve("n11");
            if (adapter is null) return Results.Problem(detail: "N11 adapter bulunamadi.", statusCode: 503);
            var categories = await adapter.GetCategoriesAsync(ct);
            return Results.Ok(new { platform = "N11", count = categories.Count, categories });
        })
        .WithName("GetN11Categories")
        .WithSummary("N11 kategori listesi")
        .Produces(200).ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/connection", async (IAdapterFactory f, CancellationToken ct) =>
        {
            var adapter = f.Resolve("n11");
            if (adapter is null) return Results.Problem(detail: "N11 adapter bulunamadi.", statusCode: 503);
            var result = await adapter.TestConnectionAsync(new Dictionary<string, string>(), ct);
            return Results.Ok(new { platform = "N11", isConnected = result.IsSuccess, storeName = result.StoreName, productCount = result.ProductCount, errorMessage = result.ErrorMessage, responseTimeMs = result.ResponseTime.TotalMilliseconds });
        })
        .WithName("TestN11Connection")
        .WithSummary("N11 API baglanti testi")
        .Produces(200).ProducesProblem(503);
    }
}
