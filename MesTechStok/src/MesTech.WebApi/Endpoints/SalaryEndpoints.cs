using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Features.Accounting.Commands.CreateSalaryRecord;
using MesTech.Application.Features.Accounting.Commands.DeleteSalaryRecord;
using MesTech.Application.Features.Accounting.Commands.UpdateSalaryRecord;
using MesTech.Application.Features.Accounting.Queries.GetSalaryRecordById;
using MesTech.Application.Features.Accounting.Queries.GetSalaryRecords;
using MesTech.Domain.Enums;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class SalaryEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/accounting/salaries")
            .WithTags("Accounting - Salaries")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/accounting/salaries — maas kayitlari listesi
        group.MapGet("/", async (
            Guid tenantId, int? year, int? month,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetSalaryRecordsQuery(tenantId, year, month), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetSalaryRecords")
        .WithSummary("Maas kayitlari listesi (yil + ay filtresi)");

        // GET /api/v1/accounting/salaries/{id} — tek maas kaydi
        group.MapGet("/{id:guid}", async (
            Guid id, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetSalaryRecordByIdQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .CacheOutput("Lookup60s")
        .WithName("GetSalaryRecordById")
        .WithSummary("Tek maas kaydi detayi");

        // POST /api/v1/accounting/salaries — yeni maas kaydi olustur
        group.MapPost("/", async (
            CreateSalaryRecordCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/accounting/salaries/{id}", new CreatedResponse(id));
        })
        .WithName("CreateSalaryRecord")
        .WithSummary("Yeni maas kaydi olustur");

        // PUT /api/v1/accounting/salaries/{id} — maas kaydi guncelle
        group.MapPut("/{id:guid}", async (
            Guid id, UpdateSalaryRecordCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var updated = command with { Id = id };
            await mediator.Send(updated, ct);
            return Results.NoContent();
        })
        .WithName("UpdateSalaryRecord")
        .WithSummary("Maas kaydi odeme durumunu guncelle");

        // DELETE /api/v1/accounting/salaries/{id} — maas kaydi sil (soft delete)
        group.MapDelete("/{id:guid}", async (
            Guid id, ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteSalaryRecordCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("DeleteSalaryRecord")
        .WithSummary("Maas kaydini sil (soft delete)");
    }
}
