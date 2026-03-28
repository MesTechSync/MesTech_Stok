using MediatR;
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
    }
}
