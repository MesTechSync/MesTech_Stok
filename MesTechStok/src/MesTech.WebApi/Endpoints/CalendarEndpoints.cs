using MesTech.Application.DTOs;
using MesTech.Application.DTOs.Accounting;
using MediatR;
using MesTech.Application.Features.Calendar.Commands.CreateCalendarEvent;
using MesTech.Application.Features.Calendar.Commands.DeleteCalendarEvent;
using MesTech.Application.Features.Calendar.Commands.GenerateTaxCalendar;
using MesTech.Application.Features.Calendar.Commands.UpdateCalendarEvent;
using MesTech.Application.Features.Calendar.Queries.GetCalendarEventById;
using MesTech.Application.Features.Calendar.Queries.GetCalendarEvents;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class CalendarEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/calendar")
            .WithTags("Calendar")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/calendar/events — takvim etkinlik listesi
        group.MapGet("/events", async (
            Guid tenantId, DateTime? from, DateTime? to,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetCalendarEventsQuery(tenantId, from, to), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetCalendarEvents")
        .WithSummary("Takvim etkinlik listesi (tarih filtresi)").Produces<IReadOnlyList<CalendarEventDto>>(200).Produces(400);

        // GET /api/v1/calendar/events/{id} — tek etkinlik
        group.MapGet("/events/{id:guid}", async (
            Guid id, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetCalendarEventByIdQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .CacheOutput("Lookup60s")
        .WithName("GetCalendarEventById")
        .WithSummary("Tek takvim etkinligi detayi").Produces<CalendarEventDto>(200).Produces(400);

        // POST /api/v1/calendar/events — yeni etkinlik oluştur
        group.MapPost("/events", async (
            CreateCalendarEventCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/calendar/events/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("CreateCalendarEvent")
        .WithSummary("Yeni takvim etkinligi olustur").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // PUT /api/v1/calendar/events/{id} — etkinlik guncelle
        group.MapPut("/events/{id:guid}", async (
            Guid id, UpdateCalendarEventCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var updated = command with { Id = id };
            await mediator.Send(updated, ct);
            return Results.NoContent();
        })
        .WithName("UpdateCalendarEvent")
        .WithSummary("Takvim etkinligi guncelle").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // DELETE /api/v1/calendar/events/{id} — etkinlik sil (soft delete)
        group.MapDelete("/events/{id:guid}", async (
            Guid id, ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteCalendarEventCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("DeleteCalendarEvent")
        .WithSummary("Takvim etkinligini sil (soft delete)").Produces(200).Produces(400);

        // POST /api/v1/calendar/generate-tax-calendar/{year} — yillik vergi takvimi olustur
        group.MapPost("/generate-tax-calendar/{year:int}", async (
            int year, Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            if (year < 2000 || year > 2100)
                return Results.Problem(detail: "Year must be between 2000 and 2100.", statusCode: 400);

            var count = await mediator.Send(
                new GenerateTaxCalendarCommand(year, tenantId), ct);
            return Results.Ok(new CalendarGenerationResponse(year, count));
        })
        .WithName("GenerateTaxCalendar")
        .WithSummary("Yillik vergi takvimi olustur (~40 etkinlik: KDV, SGK, Ba-Bs, Gecici Vergi, Yillik)").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();
    }
}
