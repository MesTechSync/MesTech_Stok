using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using MesTech.Application.DTOs;
using MesTech.Application.Commands.CreateBulkProducts;
using MesTech.Application.Commands.CreateProduct;
using MesTech.Application.Commands.DeleteProduct;
using MesTech.Application.Commands.UpdateProduct;
using MesTech.Application.Commands.UpdateProductImage;
using MesTech.Application.Features.AI.Commands.GenerateProductDescription;
using MesTech.Application.Features.Product.Queries.GetBuyboxStatus;
using MesTech.Application.Features.Product.Queries.GetPlatformProducts;
using MesTech.Application.Features.Product.Queries.GetProducts;
using MesTech.Application.Interfaces;
using MesTech.Application.Queries.SearchProductsForImageMatch;
using MesTech.Application.Queries.GetLowStockProducts;
using MesTech.Application.Queries.GetProductById;
using MesTech.Application.Queries.GetProductDbStatus;
using MesTech.WebApi.Filters;

namespace MesTech.WebApi.Endpoints;

public static class ProductEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/products").WithTags("Products").RequireRateLimiting("PerApiKey");

        // GET /api/v1/products — paginated product list (Blazor StockLot, ProductUpload, ProductVariants)
        group.MapGet("/", async (
            Guid? tenantId,
            string? search,
            Guid? categoryId,
            bool? isActive,
            int? page,
            int? pageSize,
            ISender mediator, CancellationToken ct) =>
        {
            var safeSearch = search is { Length: > 500 } ? search[..500] : search;
            var clampedSize = Math.Clamp(pageSize ?? 50, 1, 100);
            var result = await mediator.Send(
                new GetProductsQuery(tenantId ?? Guid.Empty, safeSearch, categoryId, isActive, null,
                    page ?? 1, clampedSize), ct);
            return Results.Ok(result);
        })
        .WithName("GetProductList")
        .WithSummary("Sayfalanmış ürün listesi")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/products/status — DB connectivity + counts
        group.MapGet("/status", async (ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetProductDbStatusQuery(), ct);
            return Results.Ok(result);
        })
        .WithName("GetProductStatus")
        .WithSummary("Ürün DB bağlantı durumu ve sayılar")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/products/low-stock — products below minimum stock
        group.MapGet("/low-stock", async (ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetLowStockProductsQuery(), ct);
            return Results.Ok(result);
        })
        .WithName("GetLowStockProducts")
        .WithSummary("Minimum stok altı ürünler")
        .Produces(200)
        .CacheOutput("Report120s");

        // GET /api/v1/products/{id} — get single product by ID
        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetProductByIdQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetProductById")
        .WithSummary("Tekil ürün detayı")
        .Produces(200)
        .Produces(404)
        .CacheOutput("Lookup60s");

        // POST /api/v1/products — create a new product
        group.MapPost("/", async (CreateProductCommand command, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/products/{result.ProductId}", result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("CreateProduct")
        .WithSummary("Yeni ürün oluştur")
        .AddEndpointFilter<ProductPlanLimitFilter>()
        .AddEndpointFilter<Filters.IdempotencyFilter>()
        .Produces(201)
        .Produces(400)
        .Produces(403)
        .Produces(429);

        // PUT /api/v1/products/{id} — update an existing product
        group.MapPut("/{id:guid}", async (Guid id, UpdateProductCommand command, ISender mediator, CancellationToken ct) =>
        {
            // Ensure the route ID matches the command
            var adjusted = command with { ProductId = id };
            var result = await mediator.Send(adjusted, ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("UpdateProduct")
        .WithSummary("Ürün güncelle")
        .Produces(200)
        .Produces(400);

        // DELETE /api/v1/products/{id} — soft-delete a product
        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteProductCommand(id), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("DeleteProduct")
        .WithSummary("Ürün sil (soft-delete)")
        .Produces(204)
        .Produces(400);

        // PUT /api/v1/products/{id}/image — ürün resmi güncelle
        group.MapPut("/{id:guid}/image", async (
            Guid id,
            UpdateProductImageRequest request,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new UpdateProductImageCommand(id, request.ImageUrl), ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Resim güncellenemedi"));
        })
        .WithName("UpdateProductImage")
        .WithSummary("Ürün resmi güncelle (URL)")
        .Produces(200)
        .Produces(400);

        // GET /api/v1/products/search — paginated product search with filters
        group.MapGet("/search", async (
            Guid tenantId,
            string? search,
            Guid? categoryId,
            bool? isActive,
            bool? lowStockOnly,
            int? page,
            int? pageSize,
            ISender mediator, CancellationToken ct) =>
        {
            var safeSearch = search is { Length: > 500 } ? search[..500] : search;
            var result = await mediator.Send(
                new GetProductsQuery(tenantId, safeSearch, categoryId, isActive, lowStockOnly,
                    page ?? 1, Math.Clamp(pageSize ?? 50, 1, 100)), ct);
            return Results.Ok(result);
        })
        .WithName("GetProducts")
        .WithSummary("Sayfalanmış ürün arama (kategori, stok, aktiflik filtresi)")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/products/prices — product list with price data (Blazor PriceUpdate.razor)
        group.MapGet("/prices", async (
            Guid? tenantId,
            int? page,
            int? pageSize,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetProductsQuery(tenantId ?? Guid.Empty, null, null, true, null,
                    page ?? 1, Math.Clamp(pageSize ?? 50, 1, 100)), ct);
            return Results.Ok(result);
        })
        .WithName("GetProductPrices")
        .WithSummary("Ürün fiyat listesi — toplu fiyat güncelleme için")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/products/import/templates — import template listesi (Blazor ProductImport.razor)
        group.MapGet("/import/templates", () =>
        {
            var templates = new[]
            {
                new { Id = "excel-basic", Name = "Temel Ürün (Excel)", Format = "xlsx", Columns = new[] { "SKU", "Name", "Price", "Stock", "Barcode" } },
                new { Id = "csv-full", Name = "Tam Ürün (CSV)", Format = "csv", Columns = new[] { "SKU", "Name", "Price", "Stock", "Barcode", "Category", "Brand", "Weight", "Description" } },
                new { Id = "trendyol", Name = "Trendyol Format", Format = "xlsx", Columns = new[] { "Barkod", "Model Kodu", "Stok Adedi", "Satis Fiyati", "Kategori" } },
            };
            return Results.Ok(templates);
        })
        .WithName("GetImportTemplates")
        .WithSummary("Ürün import şablon listesi")
        .Produces(200)
        .CacheOutput("Report120s");

        // POST /api/v1/products/bulk — create demo/seed products in bulk
        group.MapPost("/bulk", async (
            CreateBulkProductsCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.Message, statusCode: 400);
        })
        .WithName("CreateBulkProducts")
        .WithSummary("Toplu ürün oluştur (demo/seed amaçlı)")
        .Produces(200)
        .Produces(400);

        // GET /api/v1/products/{productId}/buybox-status — buybox durumu
        group.MapGet("/{productId:guid}/buybox-status", async (
            Guid productId, Guid tenantId, string? platformCode,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetBuyboxStatusQuery(tenantId, productId, platformCode), ct);
            return Results.Ok(result);
        })
        .WithName("GetBuyboxStatus")
        .WithSummary("Ürün buybox pozisyon durumu")
        .Produces(200)
        .CacheOutput("Report120s");

        // POST /api/v1/products/search-by-image — görsel benzerlik ile ürün arama
        group.MapPost("/search-by-image", async (
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SearchProductsForImageMatchQuery(), ct);
            return Results.Ok(result);
        })
        .WithName("SearchProductsForImageMatch")
        .WithSummary("Görsel benzerlik ile ürün arama (pgvector)")
        .Produces(200);

        // POST /api/v1/products/{productId}/generate-description — AI ürün açıklaması
        group.MapPost("/{productId:guid}/generate-description", async (
            Guid productId,
            GenerateProductDescriptionCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var adjusted = command with { ProductId = productId };
            var result = await mediator.Send(adjusted, ct);
            return Results.Ok(result);
        })
        .WithName("GenerateProductDescription")
        .WithSummary("AI ile ürün açıklaması oluştur")
        .Produces(200);
    }

    private record UpdateProductImageRequest(string ImageUrl);

    /// <summary>Buybox analiz endpoint'leri — G016 DEV 6 TUR 5.</summary>
    public static void MapBuybox(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/products/buybox")
            .WithTags("Products — Buybox")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/products/buybox/positions — buybox pozisyon listesi
        group.MapGet("/positions", async (
            Guid tenantId, string platformCode,
            IBuyboxService buybox, CancellationToken ct) =>
        {
            var result = await buybox.CheckBuyboxPositionsAsync(tenantId, platformCode, ct);
            return Results.Ok(result);
        })
        .WithName("GetBuyboxPositions")
        .WithSummary("Tüm ürünlerin buybox pozisyon listesi")
        .CacheOutput("Report120s");

        // GET /api/v1/products/buybox/lost — kaybedilen buybox'lar
        group.MapGet("/lost", async (
            Guid tenantId,
            IBuyboxService buybox, CancellationToken ct) =>
        {
            var result = await buybox.GetLostBuyboxesAsync(tenantId, ct);
            return Results.Ok(result);
        })
        .WithName("GetLostBuyboxes")
        .WithSummary("Buybox kaybedilen ürün listesi")
        .CacheOutput("Report120s");

        // GET /api/v1/products/buybox/analyze/{sku} — tekil ürün rakip analizi
        group.MapGet("/analyze/{sku}", async (
            string sku, decimal currentPrice, string platformCode,
            IBuyboxService buybox, CancellationToken ct) =>
        {
            var result = await buybox.AnalyzeCompetitorsAsync(sku, currentPrice, platformCode, ct);
            return Results.Ok(result);
        })
        .WithName("AnalyzeBuyboxCompetitors")
        .WithSummary("Tekil ürün rakip fiyat analizi ve önerilen fiyat")
        .CacheOutput("Report120s");

        // GET /api/v1/products/platform/{platformCode} — platform ürün listesi
        group.MapGet("/platform/{platformCode}", async (
            string platformCode, int? page, int? pageSize,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetPlatformProductsQuery(platformCode, page ?? 1, pageSize ?? 50), ct);
            return Results.Ok(result);
        })
        .WithName("GetPlatformProducts")
        .WithSummary("Platform ürün listesi — Trendyol, HB, N11 vb. adapter'dan çekilen ürünler")
        .Produces(200)
        .CacheOutput("Report120s");
    }
}
