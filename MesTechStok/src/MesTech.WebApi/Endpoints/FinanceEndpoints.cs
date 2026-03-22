using MediatR;
using MesTech.Application.Features.Finance.Commands.ApproveExpense;
using MesTech.Application.Features.Finance.Commands.CloseCashRegister;
using MesTech.Application.Features.Finance.Commands.MarkExpensePaid;
using MesTech.Application.Features.Finance.Queries.GetBudgetSummary;
using MesTech.Application.Features.Finance.Queries.GetCashFlow;
using MesTech.Application.Features.Finance.Queries.GetProfitLoss;

namespace MesTech.WebApi.Endpoints;

public static class FinanceEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/finance")
            .WithTags("Finance")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/finance/profit-loss — aylık kâr/zarar raporu
        group.MapGet("/profit-loss", async (
            Guid tenantId, int year, int month,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetProfitLossQuery(tenantId, year, month), ct);
            return Results.Ok(result);
        })
        .WithName("GetProfitLoss")
        .WithSummary("Aylık kâr/zarar raporu");

        // GET /api/v1/finance/cash-flow — aylık nakit akışı raporu
        group.MapGet("/cash-flow", async (
            Guid tenantId, int year, int month,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetCashFlowQuery(tenantId, year, month), ct);
            return Results.Ok(result);
        })
        .WithName("GetCashFlow")
        .WithSummary("Aylık nakit akışı raporu");

        // GET /api/v1/finance/budget-summary — bütçe özet raporu
        group.MapGet("/budget-summary", async (
            Guid tenantId, int year, int month,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetBudgetSummaryQuery(tenantId, year, month), ct);
            return Results.Ok(result);
        })
        .WithName("GetBudgetSummary")
        .WithSummary("Bütçe özet raporu");

        // POST /api/v1/finance/expenses/{id}/approve
        group.MapPost("/expenses/{id:guid}/approve", async (
            Guid id, ApproveExpenseCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(command with { ExpenseId = id }, ct);
            return Results.NoContent();
        })
        .WithName("ApproveExpense")
        .WithSummary("Masrafı onayla");

        // POST /api/v1/finance/expenses/{id}/pay
        group.MapPost("/expenses/{id:guid}/pay", async (
            Guid id, Guid bankAccountId,
            ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(new MarkExpensePaidCommand(id, bankAccountId), ct);
            return Results.NoContent();
        })
        .WithName("MarkExpensePaid")
        .WithSummary("Masrafı ödenmiş olarak işaretle");

        // POST /api/v1/finance/cash-registers/{id}/close — gün sonu kasa kapama
        group.MapPost("/cash-registers/{id:guid}/close", async (
            Guid id, CloseCashRegisterCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(command with { CashRegisterId = id }, ct);
            return Results.Ok(new
            {
                message = "Kasa gun sonu kapatildi",
                closedAt = DateTime.UtcNow
            });
        })
        .WithName("CloseCashRegister")
        .WithSummary("Kasa gun sonu — bakiye dogrulama + rapor");
    }
}
