using MediatR;
using MesTech.Application.Queries.GetSyncStatus;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class SyncStatusEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/sync-status").WithTags("SyncStatus").RequireRateLimiting("PerApiKey");

        // GET /api/v1/sync-status — get sync status (optional platform filter)
        group.MapGet("/", async (
            string? platformCode,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetSyncStatusQuery(platformCode), ct);
            return Results.Ok(result);
        })
        .WithName("GetSyncStatus")
        .WithSummary("Platform senkronizasyon durumu")
        .Produces<SyncStatusResult>(200)
        .CacheOutput("Dashboard30s");
    }
}
