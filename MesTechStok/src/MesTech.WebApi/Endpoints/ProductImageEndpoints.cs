using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Commands.UpdateProductImage;
using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

public static class ProductImageEndpoints
{
    private const string ImageBucket = "mestech-product-images";
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/products")
            .WithTags("Product Images")
            .RequireRateLimiting("PerApiKey");

        // POST /api/v1/products/{id}/images — ürün resmi yükle (binary upload)
        group.MapPost("/{id:guid}/images", async (
            Guid id,
            IFormFile file,
            IDocumentStorageService storage,
            ISender mediator,
            CancellationToken ct) =>
        {
            if (file is null || file.Length == 0)
                return Results.Problem(detail: "Dosya gerekli.", statusCode: 400);

            if (file.Length > MaxFileSizeBytes)
                return Results.Problem(detail: "Dosya boyutu 10 MB'ı aşamaz.", statusCode: 400);

            if (!AllowedContentTypes.Contains(file.ContentType))
                return Results.Problem(detail: "Desteklenen formatlar: JPEG, PNG, WebP, GIF.", statusCode: 400);

            var fileName = $"{id}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            await using var stream = file.OpenReadStream();
            var storagePath = await storage.UploadAsync(
                stream, fileName, file.ContentType, ImageBucket, ct);

            var imageUrl = await storage.GetPresignedUrlAsync(storagePath, TimeSpan.FromDays(365), ct);

            // Update product's image URL in domain
            var result = await mediator.Send(
                new UpdateProductImageCommand(id, imageUrl), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/products/{id}/images", new
                {
                    productId = id,
                    storagePath,
                    url = imageUrl,
                    contentType = file.ContentType,
                    sizeBytes = file.Length
                })
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("UploadProductImage")
        .WithSummary("Ürün resmi yükle (JPEG/PNG/WebP/GIF, max 10 MB)")
        .Produces(201).Produces(400)
        .DisableAntiforgery()
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // DELETE /api/v1/products/{id}/images — ürün resmini sil
        group.MapDelete("/{id:guid}/images", async (
            Guid id,
            string storagePath,
            IDocumentStorageService storage,
            ISender mediator,
            CancellationToken ct) =>
        {
            await storage.DeleteAsync(storagePath, ct);

            // Clear product image URL
            await mediator.Send(new UpdateProductImageCommand(id, string.Empty), ct);

            return Results.NoContent();
        })
        .WithName("DeleteProductImage")
        .WithSummary("Ürün resmini storage'dan ve üründen sil").Produces(200).Produces(400);
    }
}
