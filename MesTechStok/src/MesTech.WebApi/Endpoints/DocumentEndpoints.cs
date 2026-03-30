using MediatR;
using MesTech.Application.Features.Documents.Commands.UploadDocument;
using MesTech.Application.Features.Documents.Queries.GetDocuments;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class DocumentEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/documents")
            .WithTags("Documents")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/documents — document list (folder + paged)
        group.MapGet("/", async (
            Guid tenantId,
            Guid? folderId,
            int? page,
            int? pageSize,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetDocumentsQuery(tenantId, folderId, page ?? 1, Math.Clamp(pageSize ?? 20, 1, 100)), ct);
            return Results.Ok(result);
        })
        .WithName("GetDocuments")
        .WithSummary("Belge listesi — klasör filtreli, sayfalanmış")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // POST /api/v1/documents/upload — belge yükle (MinIO)
        group.MapPost("/upload", async (
            Guid tenantId, Guid userId,
            IFormFile file,
            Guid? folderId, string? description,
            Guid? orderId, Guid? invoiceId, Guid? productId,
            ISender mediator, CancellationToken ct) =>
        {
            if (file.Length == 0)
                return Results.BadRequest(new { error = "Dosya boş" });
            if (file.Length > 50 * 1024 * 1024) // 50MB limit (KÇ-29: ASVS V12 file size)
                return Results.BadRequest(new { error = "Dosya boyutu 50MB sınırını aşıyor" });

            await using var stream = file.OpenReadStream();
            var result = await mediator.Send(new UploadDocumentCommand(
                tenantId, userId, file.FileName, file.ContentType, file.Length, stream,
                folderId, description, orderId, invoiceId, productId), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/documents/{result.DocumentId}", result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("UploadDocument")
        .WithSummary("Belge yükle — MinIO'ya dosya yükler, Document kaydı oluşturur")
        .Produces(201).Produces(400)
        .DisableAntiforgery();
    }
}
