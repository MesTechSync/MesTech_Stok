using MediatR;
using MesTech.Application.Features.Platform.Queries.GetPlatformSyncStatus;

namespace MesTech.WebApi.Endpoints;

public static class PlatformSyncEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/platforms")
            .WithTags("Platforms")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/platforms/sync-status — platform senkronizasyon durumları
        group.MapGet("/sync-status", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetPlatformSyncStatusQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetPlatformSyncStatus")
        .WithSummary("Platform senkronizasyon durum listesi");
    }
}
