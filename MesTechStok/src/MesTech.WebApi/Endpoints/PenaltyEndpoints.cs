using MesTech.Application.DTOs;
using MesTech.Application.DTOs.Accounting;
using MediatR;
using MesTech.Application.Features.Accounting.Commands.CreatePenaltyRecord;
using MesTech.Application.Features.Accounting.Commands.DeletePenaltyRecord;
using MesTech.Application.Features.Accounting.Commands.UpdatePenaltyRecord;
using MesTech.Application.Features.Accounting.Queries.GetPenaltyRecordById;
using MesTech.Application.Features.Accounting.Queries.GetPenaltyRecords;
using MesTech.Domain.Accounting.Enums;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class PenaltyEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/accounting/penalties")
            .WithTags("Accounting - Penalties")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/accounting/penalties — ceza kayitlari listesi
        group.MapGet("/", async (
            Guid tenantId, PenaltySource? source,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetPenaltyRecordsQuery(tenantId, source), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetPenaltyRecords")
        .WithSummary("Ceza kayitlari listesi (kaynak filtresi)").Produces<IReadOnlyList<PenaltyRecordDto>>(200).Produces(400);

        // GET /api/v1/accounting/penalties/{id} — tek ceza kaydi
        group.MapGet("/{id:guid}", async (
            Guid id, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPenaltyRecordByIdQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .CacheOutput("Lookup60s")
        .WithName("GetPenaltyRecordById")
        .WithSummary("Tek ceza kaydi detayi").Produces<PenaltyRecordDto>(200).Produces(400);

        // POST /api/v1/accounting/penalties — yeni ceza kaydi olustur
        group.MapPost("/", async (
            CreatePenaltyRecordCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/accounting/penalties/{id}", new CreatedResponse(id));
        })
        .WithName("CreatePenaltyRecord")
        .WithSummary("Yeni ceza kaydi olustur").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // PUT /api/v1/accounting/penalties/{id} — ceza kaydi guncelle
        group.MapPut("/{id:guid}", async (
            Guid id, UpdatePenaltyRecordCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var updated = command with { Id = id };
            await mediator.Send(updated, ct);
            return Results.NoContent();
        })
        .WithName("UpdatePenaltyRecord")
        .WithSummary("Ceza kaydi odeme durumunu guncelle").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // DELETE /api/v1/accounting/penalties/{id} — ceza kaydi sil (soft delete)
        group.MapDelete("/{id:guid}", async (
            Guid id, ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeletePenaltyRecordCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("DeletePenaltyRecord")
        .WithSummary("Ceza kaydini sil (soft delete)").Produces(200).Produces(400);
    }
}
