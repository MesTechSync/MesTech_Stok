using MediatR;
using MesTech.Application.Commands.CreateIncome;
using MesTech.Application.Commands.DeleteIncome;
using MesTech.Application.Commands.UpdateIncome;
using MesTech.Application.Queries.GetExpenses;
using MesTech.Application.Queries.GetIncomeById;
using MesTech.Application.Queries.GetIncomes;
using MesTech.Domain.Enums;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class IncomeEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/accounting/incomes")
            .WithTags("Accounting - Incomes")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/accounting/incomes — gelir kayitlari listesi
        group.MapGet("/", async (
            DateTime? from, DateTime? to, IncomeType? type, Guid? tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetIncomesQuery(from, to, type, tenantId), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetIncomes")
        .WithSummary("Gelir kayitlari listesi (tarih + tip filtresi)");

        // GET /api/v1/accounting/incomes/{id} — tek gelir kaydi
        group.MapGet("/{id:guid}", async (
            Guid id, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetIncomeByIdQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .CacheOutput("Lookup60s")
        .WithName("GetIncomeById")
        .WithSummary("Tek gelir kaydi detayi");

        // POST /api/v1/accounting/incomes — yeni gelir kaydi olustur
        group.MapPost("/", async (
            CreateIncomeCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/accounting/incomes/{id}", new { id });
        })
        .WithName("CreateIncome")
        .WithSummary("Yeni gelir kaydi olustur");

        // PUT /api/v1/accounting/incomes/{id} — gelir kaydi guncelle
        group.MapPut("/{id:guid}", async (
            Guid id, UpdateIncomeCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var updated = command with { Id = id };
            await mediator.Send(updated, ct);
            return Results.NoContent();
        })
        .WithName("UpdateIncome")
        .WithSummary("Gelir kaydi guncelle");

        // DELETE /api/v1/accounting/incomes/{id} — gelir kaydi sil (soft delete)
        group.MapDelete("/{id:guid}", async (
            Guid id, ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteIncomeCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("DeleteIncome")
        .WithSummary("Gelir kaydini sil (soft delete)");
    }
}
