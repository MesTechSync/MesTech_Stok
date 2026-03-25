using MediatR;
using MesTech.Application.Commands.CreateProduct;
using MesTech.Application.Commands.DeleteProduct;
using MesTech.Application.Commands.UpdateProduct;
using MesTech.Application.Commands.UpdateProductImage;
using MesTech.Application.Queries.GetLowStockProducts;
using MesTech.Application.Queries.GetProductById;
using MesTech.Application.Queries.GetProductDbStatus;

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
        .WithSummary("Ürün DB bağlantı durumu ve sayılar");

        // GET /api/v1/products/low-stock — products below minimum stock
        group.MapGet("/low-stock", async (ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetLowStockProductsQuery(), ct);
            return Results.Ok(result);
        })
        .WithName("GetLowStockProducts")
        .WithSummary("Minimum stok altı ürünler");

        // GET /api/v1/products/{id} — get single product by ID
        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetProductByIdQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetProductById")
        .WithSummary("Tekil ürün detayı");

        // POST /api/v1/products — create a new product
        group.MapPost("/", async (CreateProductCommand command, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/products/{result.ProductId}", result)
                : Results.BadRequest(new { result.ErrorMessage });
        })
        .WithName("CreateProduct")
        .WithSummary("Yeni ürün oluştur");

        // PUT /api/v1/products/{id} — update an existing product
        group.MapPut("/{id:guid}", async (Guid id, UpdateProductCommand command, ISender mediator, CancellationToken ct) =>
        {
            // Ensure the route ID matches the command
            var adjusted = command with { ProductId = id };
            var result = await mediator.Send(adjusted, ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.BadRequest(new { result.ErrorMessage });
        })
        .WithName("UpdateProduct")
        .WithSummary("Ürün güncelle");

        // DELETE /api/v1/products/{id} — soft-delete a product
        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteProductCommand(id), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { result.ErrorMessage });
        })
        .WithName("DeleteProduct")
        .WithSummary("Ürün sil (soft-delete)");

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
                : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .WithName("UpdateProductImage")
        .WithSummary("Ürün resmi güncelle (URL)");
    }

    private record UpdateProductImageRequest(string ImageUrl);
}
