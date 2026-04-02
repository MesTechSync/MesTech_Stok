using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Amazon TR platform endpoint'leri — Swagger tag: "Amazon" (G10821-DEV6).
/// </summary>
public static class AmazonEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/amazon")
            .WithTags("Amazon")
            .RequireAuthorization()
            .RequireRateLimiting("PerApiKey");

        group.MapGet("/products", async (IAdapterFactory f, CancellationToken ct) =>
        {
            var adapter = f.Resolve("amazon");
            if (adapter is null) return Results.Problem(detail: "Amazon adapter bulunamadi.", statusCode: 503);
            var products = await adapter.PullProductsAsync(ct);
            return Results.Ok(new { platform = "Amazon", count = products.Count, products });
        })
        .WithName("GetAmazonProducts")
        .WithSummary("Amazon TR urunlerini cek")
        .Produces(200).ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/categories", async (IAdapterFactory f, CancellationToken ct) =>
        {
            var adapter = f.Resolve("amazon");
            if (adapter is null) return Results.Problem(detail: "Amazon adapter bulunamadi.", statusCode: 503);
            var categories = await adapter.GetCategoriesAsync(ct);
            return Results.Ok(new { platform = "Amazon", count = categories.Count, categories });
        })
        .WithName("GetAmazonCategories")
        .WithSummary("Amazon kategori listesi")
        .Produces(200).ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/connection", async (IAdapterFactory f, CancellationToken ct) =>
        {
            var adapter = f.Resolve("amazon");
            if (adapter is null) return Results.Problem(detail: "Amazon adapter bulunamadi.", statusCode: 503);
            var result = await adapter.TestConnectionAsync(new Dictionary<string, string>(), ct);
            return Results.Ok(new { platform = "Amazon", isConnected = result.IsSuccess, storeName = result.StoreName, productCount = result.ProductCount, errorMessage = result.ErrorMessage, responseTimeMs = result.ResponseTime.TotalMilliseconds });
        })
        .WithName("TestAmazonConnection")
        .WithSummary("Amazon API baglanti testi")
        .Produces(200).ProducesProblem(503);
    }
}
