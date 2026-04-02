using MediatR;
using MesTech.Application.Features.Settings.Commands.SaveImportTemplate;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Import şablon yönetimi endpoint.
/// DEV6 TUR13: Ayrı dosya (linter workaround).
/// </summary>
public static class ImportTemplateEndpoint
{
    public static void Map(WebApplication app)
    {
        // POST /api/v1/settings/import-template — şablon kaydet
        app.MapPost("/api/v1/settings/import-template", async (
            SaveImportTemplateCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("SaveImportTemplate")
        .WithSummary("Import şablon kaydet — Excel/CSV sütun mapping tanımla")
        .WithTags("Settings")
        .RequireRateLimiting("PerApiKey")
        .Produces(200)
        .Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();
    }
}
