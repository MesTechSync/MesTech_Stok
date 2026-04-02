using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// WooCommerce platform endpoint'leri — Swagger'da görünür (G10821).
/// GET  /api/v1/woocommerce/products    — WooCommerce ürün listesi
/// GET  /api/v1/woocommerce/categories  — WooCommerce kategori listesi
/// GET  /api/v1/woocommerce/connection  — WooCommerce bağlantı testi
/// </summary>
public static class WooCommerceEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/woocommerce")
            .WithTags("WooCommerce")
            .RequireAuthorization()
            .RequireRateLimiting("PerApiKey");

        group.MapGet("/products", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("woocommerce");
            if (adapter is null)
                return Results.Problem(
                    detail: "WooCommerce adapter bulunamadi — DI kaydi kontrol edin.",
                    statusCode: 503);

            var products = await adapter.PullProductsAsync(ct);
            return Results.Ok(new
            {
                platform = "WooCommerce",
                count = products.Count,
                products
            });
        })
        .WithName("GetWooCommerceProducts")
        .WithSummary("WooCommerce urunlerini cek")
        .Produces(200)
        .ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/categories", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("woocommerce");
            if (adapter is null)
                return Results.Problem(detail: "WooCommerce adapter bulunamadi.", statusCode: 503);

            var categories = await adapter.GetCategoriesAsync(ct);
            return Results.Ok(new
            {
                platform = "WooCommerce",
                count = categories.Count,
                categories
            });
        })
        .WithName("GetWooCommerceCategories")
        .WithSummary("WooCommerce kategori listesi")
        .Produces(200)
        .ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/connection", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var adapter = adapterFactory.Resolve("woocommerce");
            if (adapter is null)
                return Results.Problem(detail: "WooCommerce adapter bulunamadi.", statusCode: 503);

            var result = await adapter.TestConnectionAsync(new Dictionary<string, string>(), ct);
            return Results.Ok(new
            {
                platform = "WooCommerce",
                isConnected = result.IsSuccess,
                storeName = result.StoreName,
                productCount = result.ProductCount,
                errorMessage = result.ErrorMessage,
                responseTimeMs = result.ResponseTime.TotalMilliseconds
            });
        })
        .WithName("TestWooCommerceConnection")
        .WithSummary("WooCommerce API baglanti testi")
        .Produces(200)
        .ProducesProblem(503);
    }
}
