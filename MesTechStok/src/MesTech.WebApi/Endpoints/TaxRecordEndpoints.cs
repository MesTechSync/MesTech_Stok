using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Features.Accounting.Commands.CreateTaxRecord;
using MesTech.Application.Features.Accounting.Commands.DeleteTaxRecord;
using MesTech.Application.Features.Accounting.Commands.UpdateTaxRecord;
using MesTech.Application.Features.Accounting.Queries.GetTaxRecordById;
using MesTech.Application.Features.Accounting.Queries.GetTaxRecords;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class TaxRecordEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/accounting/taxes")
            .WithTags("Accounting - Tax Records")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/accounting/taxes — vergi kayitlari listesi
        group.MapGet("/", async (
            Guid tenantId, string? taxType, int? year,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetTaxRecordsQuery(tenantId, taxType, year), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetTaxRecords")
        .WithSummary("Vergi kayitlari listesi (tip + yil filtresi)").Produces(200).Produces(400);

        // GET /api/v1/accounting/taxes/{id} — tek vergi kaydi
        group.MapGet("/{id:guid}", async (
            Guid id, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetTaxRecordByIdQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .CacheOutput("Lookup60s")
        .WithName("GetTaxRecordById")
        .WithSummary("Tek vergi kaydi detayi").Produces(200).Produces(400);

        // POST /api/v1/accounting/taxes — yeni vergi kaydi olustur
        group.MapPost("/", async (
            CreateTaxRecordCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/accounting/taxes/{id}", new CreatedResponse(id));
        })
        .WithName("CreateTaxRecord")
        .WithSummary("Yeni vergi kaydi olustur").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // PUT /api/v1/accounting/taxes/{id} — vergi kaydi guncelle
        group.MapPut("/{id:guid}", async (
            Guid id, UpdateTaxRecordCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var updated = command with { Id = id };
            await mediator.Send(updated, ct);
            return Results.NoContent();
        })
        .WithName("UpdateTaxRecord")
        .WithSummary("Vergi kaydi odeme durumunu guncelle").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // DELETE /api/v1/accounting/taxes/{id} — vergi kaydi sil (soft delete)
        group.MapDelete("/{id:guid}", async (
            Guid id, ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteTaxRecordCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("DeleteTaxRecord")
        .WithSummary("Vergi kaydini sil (soft delete)").Produces(200).Produces(400);
    }
}
