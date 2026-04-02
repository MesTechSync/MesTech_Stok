using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Features.System.UserNotifications.Commands.MarkAllNotificationsRead;
using MesTech.Application.Features.System.UserNotifications.Commands.MarkNotificationRead;
using MesTech.Application.Features.System.UserNotifications.Queries.GetUnreadNotificationCount;
using MesTech.Application.Features.System.UserNotifications.Queries.GetUserNotifications;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class UserNotificationEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/system/notifications")
            .WithTags("UserNotifications")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/system/notifications?userId=&isRead= — kullanici bildirimleri
        group.MapGet("/", async (
            Guid tenantId, Guid userId, int? page, int? pageSize, bool? unreadOnly,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetUserNotificationsQuery(
                    tenantId,
                    userId,
                    page is > 0 ? page.Value : 1,
                    Math.Clamp(pageSize is > 0 ? pageSize.Value : 20, 1, 100),
                    unreadOnly ?? false), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetUserNotifications")
        .WithSummary("Kullanici ici bildirim listesi (sayfalama + okunmamis filtresi)").Produces(200).Produces(400);

        // PUT /api/v1/system/notifications/{id}/read — bildirimi okundu isaretle
        group.MapPut("/{id:guid}/read", async (
            Guid id, Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var success = await mediator.Send(
                new MarkUserNotificationReadCommand(tenantId, id), ct);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("MarkUserNotificationRead")
        .WithSummary("Kullanici ici bildirimi okundu olarak isaretler").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // PUT /api/v1/system/notifications/read-all?userId= — tum bildirimleri okundu isaretle
        group.MapPut("/read-all", async (
            Guid tenantId, Guid userId,
            ISender mediator, CancellationToken ct) =>
        {
            var count = await mediator.Send(
                new MarkAllUserNotificationsReadCommand(tenantId, userId), ct);
            return Results.Ok(new StatusResponse("Ok", $"{count}"));
        })
        .WithName("MarkAllUserNotificationsRead")
        .WithSummary("Kullanicinin tum bildirimlerini okundu olarak isaretler").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/system/notifications/unread-count?userId= — okunmamis bildirim sayisi
        group.MapGet("/unread-count", async (
            Guid tenantId, Guid userId,
            ISender mediator, CancellationToken ct) =>
        {
            var count = await mediator.Send(
                new GetUnreadNotificationCountQuery(tenantId, userId), ct);
            return Results.Ok(new StatusResponse("Ok", $"{count}"));
        })
        .CacheOutput("Lookup60s")
        .WithName("GetUnreadUserNotificationCount")
        .WithSummary("Kullanicinin okunmamis bildirim sayisi").Produces(200).Produces(400);
    }
}
