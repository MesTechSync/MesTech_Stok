using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Features.Notifications.Commands.UpdateNotificationSettings;
using MesTech.Application.Features.Notifications.Queries.GetNotificationSettings;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class NotificationSettingEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/system/notification-settings")
            .WithTags("NotificationSettings")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/system/notification-settings?userId= — bildirim ayarlari
        group.MapGet("/", async (
            Guid tenantId, Guid userId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetNotificationSettingsQuery(tenantId, userId), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetNotificationSettings")
        .WithSummary("Kullanici bildirim ayarlarini getirir");

        // PUT /api/v1/system/notification-settings — bildirim ayarlari guncelle
        group.MapPut("/", async (
            UpdateNotificationSettingsCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Ok(new CreatedResponse(id));
        })
        .WithName("UpdateNotificationSettings")
        .WithSummary("Kullanici bildirim ayarlarini gunceller (upsert)");
    }
}
