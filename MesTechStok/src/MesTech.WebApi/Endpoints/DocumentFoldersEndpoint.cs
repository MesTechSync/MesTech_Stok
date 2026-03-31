using MediatR;
using MesTech.Application.Features.Documents.Queries.GetDocumentFolders;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Belge klasör yapısı endpoint.
/// DEV6 TUR13: Ayrı dosya (linter workaround).
/// </summary>
public static class DocumentFoldersEndpoint
{
    public static void Map(WebApplication app)
    {
        // GET /api/v1/documents/folders — klasör listesi
        app.MapGet("/api/v1/documents/folders", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetDocumentFoldersQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetDocumentFolders")
        .WithSummary("Belge klasör listesi — organizasyon yapısı")
        .WithTags("Documents")
        .RequireRateLimiting("PerApiKey")
        .Produces(200);
    }
}
