using MediatR;
using MesTech.Application.Commands.SyncPlatform;
using MesTech.Application.Features.Platform.Queries.GetPlatformSyncStatus;
using MesTech.Domain.Enums;

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
        .WithSummary("Platform senkronizasyon durum listesi").Produces(200).Produces(400);

        // POST /api/v1/platforms/{platformCode}/sync — platform senkronizasyonu başlat
        group.MapPost("/{platformCode}/sync", async (
            string platformCode,
            SyncDirection? direction,
            DateTime? since,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new SyncPlatformCommand(platformCode, direction ?? SyncDirection.Bidirectional, since), ct);
            return Results.Ok(result);
        })
        .WithName("SyncPlatform")
        .WithSummary("Belirtilen platformu senkronize et (* = tümü)").Produces(200).Produces(400);
    }
}
