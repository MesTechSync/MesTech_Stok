using MesTech.Application.DTOs;
using MesTech.Application.DTOs.Accounting;
using MediatR;
using MesTech.Application.Features.Accounting.Commands.CreateFixedExpense;
using MesTech.Application.Features.Accounting.Commands.DeleteFixedExpense;
using MesTech.Application.Features.Accounting.Commands.UpdateFixedExpense;
using MesTech.Application.Features.Accounting.Queries.GetFixedExpenseById;
using MesTech.Application.Features.Accounting.Queries.GetFixedExpenses;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class FixedExpenseEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/accounting/fixed-expenses")
            .WithTags("Accounting - Fixed Expenses")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/accounting/fixed-expenses — sabit gider listesi
        group.MapGet("/", async (
            Guid tenantId, bool? isActive,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetFixedExpensesQuery(tenantId, isActive), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetFixedExpenses")
        .WithSummary("Sabit gider listesi (aktif/pasif filtresi)").Produces<IReadOnlyList<FixedExpenseDto>>(200).Produces(400);

        // GET /api/v1/accounting/fixed-expenses/{id} — tek sabit gider
        group.MapGet("/{id:guid}", async (
            Guid id, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetFixedExpenseByIdQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .CacheOutput("Lookup60s")
        .WithName("GetFixedExpenseById")
        .WithSummary("Tek sabit gider detayi").Produces<FixedExpenseDto>(200).Produces(400);

        // POST /api/v1/accounting/fixed-expenses — yeni sabit gider olustur
        group.MapPost("/", async (
            CreateFixedExpenseCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/accounting/fixed-expenses/{id}", new CreatedResponse(id));
        })
        .WithName("CreateFixedExpense")
        .WithSummary("Yeni sabit gider olustur").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // PUT /api/v1/accounting/fixed-expenses/{id} — sabit gider guncelle
        group.MapPut("/{id:guid}", async (
            Guid id, UpdateFixedExpenseCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var updated = command with { Id = id };
            await mediator.Send(updated, ct);
            return Results.NoContent();
        })
        .WithName("UpdateFixedExpense")
        .WithSummary("Sabit gider guncelle (tutar / aktiflik)").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // DELETE /api/v1/accounting/fixed-expenses/{id} — sabit gider sil (soft delete)
        group.MapDelete("/{id:guid}", async (
            Guid id, ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteFixedExpenseCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("DeleteFixedExpense")
        .WithSummary("Sabit gideri sil (soft delete)").Produces(200).Produces(400);
    }
}
