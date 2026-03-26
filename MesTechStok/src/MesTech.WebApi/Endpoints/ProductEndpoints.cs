using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using MesTech.Application.DTOs;
using MesTech.Application.Commands.CreateProduct;
using MesTech.Application.Commands.DeleteProduct;
using MesTech.Application.Commands.UpdateProduct;
using MesTech.Application.Commands.UpdateProductImage;
using MesTech.Application.Interfaces;
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
    }
}
