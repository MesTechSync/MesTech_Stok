using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Features.Notifications.Commands.MarkNotificationRead;
using MesTech.Application.Features.Notifications.Commands.SendNotification;
using MesTech.Application.Features.Notifications.Queries.GetNotifications;
using MesTech.WebApi.Hubs;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.SignalR;

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
                    Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 100),
                    unreadOnly ?? false), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetNotifications")
        .WithSummary("Bildirim listesi (sayfalama + okunmamış filtresi)").Produces(200).Produces(400);

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
        .WithSummary("Bildirimi okundu olarak işaretle").Produces(200).Produces(400);

        // POST /api/v1/notifications/send — bildirim gönder
        group.MapPost("/send", async (
            SendNotificationCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/notifications/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("SendNotification")
        .WithSummary("Bildirim gönder (kanal + şablon bazlı)").Produces(200).Produces(400);

        // GET /api/v1/notifications/unread-count — okunmamış bildirim sayısı
        group.MapGet("/unread-count", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetNotificationsQuery(tenantId, Page: 1, PageSize: 1, UnreadOnly: true), ct);
            return Results.Ok(new StatusResponse("Ok", $"{result.TotalCount}"));
        })
        .CacheOutput("Lookup60s")
        .WithName("GetUnreadNotificationCount")
        .WithSummary("Okunmamış bildirim sayısı").Produces(200).Produces(400);

        // POST /api/v1/notifications/push — bildirim gönder + SignalR real-time broadcast
        group.MapPost("/push", async (
            PushNotificationRequest request,
            ISender mediator,
            IHubContext<MesTechHub> hubContext,
            CancellationToken ct) =>
        {
            // 1. MESA Bot'a gönder (persistent)
            var id = await mediator.Send(new SendNotificationCommand(
                request.TenantId, request.Channel ?? "Push",
                request.Recipient ?? "all", request.TemplateName ?? "custom",
                request.Message), ct);

            // 2. SignalR real-time push (instant)
            await MesTechHub.PushNotification(
                hubContext, request.TenantId.ToString(),
                request.Title, request.Message,
                request.Category ?? "System", request.ActionUrl);

            return Results.Created($"/api/v1/notifications/{id}", new CreatedResponse(id));
        })
        .WithName("PushNotification")
        .WithSummary("Bildirim gönder + SignalR real-time push (V5)").Produces(200).Produces(400);
    }

    internal record PushNotificationRequest(
        Guid TenantId,
        string Title,
        string Message,
        string? Channel = null,
        string? Recipient = null,
        string? TemplateName = null,
        string? Category = null,
        string? ActionUrl = null);
}
