using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Generic platform endpoint helper — DRY: 13 platform × 3 endpoint = 1 helper.
/// Her platform dosyası bunu çağırarak products/categories/connection endpoint'lerini register eder.
/// </summary>
public static class PlatformEndpointHelper
{
    /// <summary>
    /// Register 3 standard endpoints for a platform:
    /// GET /api/v1/{route}/products, /categories, /connection
    /// </summary>
    public static void MapPlatformEndpoints(
        WebApplication app,
        string route,
        string platformCode,
        string displayName)
    {
        var group = app.MapGroup($"/api/v1/{route}")
            .WithTags(displayName)
            .RequireAuthorization()
            .RequireRateLimiting("PerApiKey");

        group.MapGet("/products", async (IAdapterFactory f, CancellationToken ct) =>
        {
            var adapter = f.Resolve(platformCode);
            if (adapter is null)
                return Results.Problem(detail: $"{displayName} adapter bulunamadi.", statusCode: 503);
            var products = await adapter.PullProductsAsync(ct);
            return Results.Ok(new { platform = displayName, count = products.Count, products });
        })
        .WithName($"Get{displayName.Replace(" ", "")}Products")
        .WithSummary($"{displayName} urunlerini cek")
        .Produces(200).ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/categories", async (IAdapterFactory f, CancellationToken ct) =>
        {
            var adapter = f.Resolve(platformCode);
            if (adapter is null)
                return Results.Problem(detail: $"{displayName} adapter bulunamadi.", statusCode: 503);
            var categories = await adapter.GetCategoriesAsync(ct);
            return Results.Ok(new { platform = displayName, count = categories.Count, categories });
        })
        .WithName($"Get{displayName.Replace(" ", "")}Categories")
        .WithSummary($"{displayName} kategori listesi")
        .Produces(200).ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/connection", async (IAdapterFactory f, CancellationToken ct) =>
        {
            var adapter = f.Resolve(platformCode);
            if (adapter is null)
                return Results.Problem(detail: $"{displayName} adapter bulunamadi.", statusCode: 503);
            var result = await adapter.TestConnectionAsync(new Dictionary<string, string>(), ct);
            return Results.Ok(new
            {
                platform = displayName,
                isConnected = result.IsSuccess,
                storeName = result.StoreName,
                productCount = result.ProductCount,
                errorMessage = result.ErrorMessage,
                responseTimeMs = result.ResponseTime.TotalMilliseconds
            });
        })
        .WithName($"Test{displayName.Replace(" ", "")}Connection")
        .WithSummary($"{displayName} API baglanti testi")
        .Produces(200).ProducesProblem(503);
    }
}
