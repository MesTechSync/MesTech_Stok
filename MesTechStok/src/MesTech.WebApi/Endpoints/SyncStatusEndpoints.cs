using MediatR;
using MesTech.Application.Queries.GetSyncStatus;

namespace MesTech.WebApi.Endpoints;

public static class SyncStatusEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/sync-status").WithTags("SyncStatus");

        // GET /api/v1/sync-status — get sync status (optional platform filter)
        group.MapGet("/", async (
            string? platformCode,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetSyncStatusQuery(platformCode), ct);
            return Results.Ok(result);
        });
    }
}
