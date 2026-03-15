using MediatR;
using MesTech.Application.Features.Notifications.Commands.MarkNotificationRead;
using MesTech.Application.Features.Notifications.Commands.SendNotification;
using MesTech.Application.Features.Notifications.Queries.GetNotifications;

namespace MesTech.WebApi.Endpoints;

public static class NotificationEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/notifications")
            .WithTags("Notifications")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/notifications — bildirim listesi
        group.MapGet("/", async (
            Guid tenantId, int page, int pageSize, bool? unreadOnly,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetNotificationsQuery(
                    tenantId,
                    page <= 0 ? 1 : page,
                    pageSize <= 0 ? 20 : pageSize,
                    unreadOnly ?? false), ct);
            return Results.Ok(result);
        })
        .WithName("GetNotifications")
        .WithSummary("Bildirim listesi (sayfalama + okunmamış filtresi)");

        // POST /api/v1/notifications/{id}/read — bildirimi okundu olarak işaretle
        group.MapPost("/{id:guid}/read", async (
            Guid id, Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var success = await mediator.Send(
                new MarkNotificationReadCommand(tenantId, id), ct);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("MarkNotificationRead")
        .WithSummary("Bildirimi okundu olarak işaretle");

        // POST /api/v1/notifications/send — bildirim gönder
        group.MapPost("/send", async (
            SendNotificationCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/notifications/{id}", new { id });
        })
        .WithName("SendNotification")
        .WithSummary("Bildirim gönder (kanal + şablon bazlı)");

        // GET /api/v1/notifications/unread-count — okunmamış bildirim sayısı
        group.MapGet("/unread-count", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetNotificationsQuery(tenantId, Page: 1, PageSize: 1, UnreadOnly: true), ct);
            return Results.Ok(new { unreadCount = result.TotalCount });
        })
        .WithName("GetUnreadNotificationCount")
        .WithSummary("Okunmamış bildirim sayısı");
    }
}
