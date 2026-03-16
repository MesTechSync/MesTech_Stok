using MediatR;
using MesTech.Application.Features.Calendar.Commands.CreateCalendarEvent;

namespace MesTech.WebApi.Endpoints;

public static class CalendarEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/calendar")
            .WithTags("Calendar")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/calendar/events — takvim etkinlik listesi
        // DEV1-DEPENDENCY: GetCalendarEventsQuery not yet available
        group.MapGet("/events", (
            Guid? tenantId, DateTime? from, DateTime? to) =>
            Results.Ok(new
            {
                Message = "Calendar events list endpoint — DEV1 GetCalendarEventsQuery pending",
                TenantId = tenantId,
                From = from,
                To = to,
                Items = Array.Empty<object>(),
                TotalCount = 0,
                Status = "not_implemented"
            }))
        .WithName("GetCalendarEvents")
        .WithSummary("Takvim etkinlik listesi (DEV1-DEPENDENCY)");

        // POST /api/v1/calendar/events — yeni etkinlik oluştur
        group.MapPost("/events", async (
            CreateCalendarEventCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/calendar/events/{id}", new { id });
        })
        .WithName("CreateCalendarEvent")
        .WithSummary("Yeni takvim etkinliği oluştur");
    }
}
