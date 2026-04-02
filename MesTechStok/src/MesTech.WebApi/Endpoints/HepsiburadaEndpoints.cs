using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Hepsiburada platform endpoint'leri — Swagger tag: "Hepsiburada" (G10821-DEV6).
/// </summary>
public static class HepsiburadaEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/hepsiburada")
            .WithTags("Hepsiburada")
            .RequireAuthorization()
            .RequireRateLimiting("PerApiKey");

        group.MapGet("/products", async (IAdapterFactory f, CancellationToken ct) =>
        {
            var adapter = f.Resolve("hepsiburada");
            if (adapter is null) return Results.Problem(detail: "Hepsiburada adapter bulunamadi.", statusCode: 503);
            var products = await adapter.PullProductsAsync(ct);
            return Results.Ok(new { platform = "Hepsiburada", count = products.Count, products });
        })
        .WithName("GetHepsiburadaProducts")
        .WithSummary("Hepsiburada urunlerini cek")
        .Produces(200).ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/categories", async (IAdapterFactory f, CancellationToken ct) =>
        {
            var adapter = f.Resolve("hepsiburada");
            if (adapter is null) return Results.Problem(detail: "Hepsiburada adapter bulunamadi.", statusCode: 503);
            var categories = await adapter.GetCategoriesAsync(ct);
            return Results.Ok(new { platform = "Hepsiburada", count = categories.Count, categories });
        })
        .WithName("GetHepsiburadaCategories")
        .WithSummary("Hepsiburada kategori listesi")
        .Produces(200).ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/connection", async (IAdapterFactory f, CancellationToken ct) =>
        {
            var adapter = f.Resolve("hepsiburada");
            if (adapter is null) return Results.Problem(detail: "Hepsiburada adapter bulunamadi.", statusCode: 503);
            var result = await adapter.TestConnectionAsync(new Dictionary<string, string>(), ct);
            return Results.Ok(new { platform = "Hepsiburada", isConnected = result.IsSuccess, storeName = result.StoreName, productCount = result.ProductCount, errorMessage = result.ErrorMessage, responseTimeMs = result.ResponseTime.TotalMilliseconds });
        })
        .WithName("TestHepsiburadaConnection")
        .WithSummary("Hepsiburada API baglanti testi")
        .Produces(200).ProducesProblem(503);
    }
}
