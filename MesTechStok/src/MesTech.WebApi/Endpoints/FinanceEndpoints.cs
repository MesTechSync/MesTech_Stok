using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using MesTech.Application.DTOs;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseSummary;
using MesTech.Application.Features.Finance.Queries.GetBankAccounts;
using MesTech.Application.Features.Finance.Commands.ApproveExpense;
using MesTech.Application.Features.Finance.Commands.CloseCashRegister;
using MesTech.Application.Features.Finance.Commands.MarkExpensePaid;
using MesTech.Application.Features.Finance.Queries.GetBudgetSummary;
using MesTech.Application.Features.Finance.Queries.GetCashFlow;
using MesTech.Application.Features.Finance.Commands.CreateCashRegister;
using MesTech.Application.Features.Finance.Commands.CreateExpense;
using MesTech.Application.Features.Finance.Commands.RecordCashTransaction;
using MesTech.Application.Features.Finance.Queries.GetCashRegisters;
using MesTech.Application.Features.Finance.Queries.GetProfitLoss;
using MesTech.Application.Queries.GetExpenses;
using MesTech.Application.Queries.GetKarZarar;

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
        .WithSummary("Aylık kâr/zarar raporu")
        .Produces(200)
        .CacheOutput("Report120s");

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
        .WithSummary("Aylık nakit akışı raporu")
        .Produces(200)
        .CacheOutput("Report120s");

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
        .WithSummary("Bütçe özet raporu")
        .Produces(200)
        .CacheOutput("Report120s");

        // POST /api/v1/finance/expenses/{id}/approve
        group.MapPost("/expenses/{id:guid}/approve", async (
            Guid id, ApproveExpenseCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(command with { ExpenseId = id }, ct);
            return Results.NoContent();
        })
        .WithName("ApproveExpense")
        .AddEndpointFilter<Filters.IdempotencyFilter>()
        .WithSummary("Masrafı onayla").Produces(200).Produces(400);

        // POST /api/v1/finance/expenses/{id}/pay
        group.MapPost("/expenses/{id:guid}/pay", async (
            Guid id, Guid bankAccountId,
            ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(new MarkExpensePaidCommand(id, bankAccountId), ct);
            return Results.NoContent();
        })
        .WithName("MarkExpensePaid")
        .AddEndpointFilter<Filters.IdempotencyFilter>()
        .WithSummary("Masrafı ödenmiş olarak işaretle").Produces(200).Produces(400);

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
        .AddEndpointFilter<Filters.IdempotencyFilter>()
        .WithSummary("Kasa gun sonu — bakiye dogrulama + rapor").Produces(200).Produces(400);

        // ─── DEFTER KAPATMA: 4 eksik finance endpoint [ENT-DEV6] ───

        // GET /api/v1/finance/cash-registers — kasa listesi
        group.MapGet("/cash-registers", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetCashRegistersQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetCashRegisters")
        .WithSummary("Kasa listesi")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // POST /api/v1/finance/cash-registers — yeni kasa oluştur
        group.MapPost("/cash-registers", async (
            CreateCashRegisterCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/finance/cash-registers/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("CreateCashRegister")
        .AddEndpointFilter<Filters.IdempotencyFilter>()
        .WithSummary("Yeni kasa tanımla").Produces(200).Produces(400);

        // POST /api/v1/finance/expenses — yeni masraf kaydı
        group.MapPost("/expenses", async (
            CreateExpenseCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/finance/expenses/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("CreateExpense")
        .AddEndpointFilter<Filters.IdempotencyFilter>()
        .WithSummary("Yeni masraf kaydı oluştur").Produces(200).Produces(400);

        // POST /api/v1/finance/cash-transactions — kasa hareketi kaydet
        group.MapPost("/cash-transactions", async (
            RecordCashTransactionCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/finance/cash-transactions/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("RecordCashTransaction")
        .AddEndpointFilter<Filters.IdempotencyFilter>()
        .WithSummary("Kasa hareketi kaydet (giriş/çıkış)").Produces(200).Produces(400);

        // ─── GELIR/GIDER ENDPOINT'LERI [ENT-DEV6] ───

        // GET /api/v1/finance/expenses/list — masraf listesi (filtreleme)
        group.MapGet("/expenses/list", async (
            DateTime? from, DateTime? to, int? type, Guid? tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetExpensesQuery(from, to,
                    type.HasValue ? (MesTech.Domain.Enums.ExpenseType)type.Value : null,
                    tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetExpenses")
        .WithSummary("Masraf listesi (tarih + tip filtresi)")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/finance/income-expenses — gelir/gider listesi
        group.MapGet("/income-expenses", async (
            Guid tenantId, string? type, DateTime? from, DateTime? to, int page, int pageSize,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetIncomeExpenseListQuery(tenantId, type, from, to, Math.Max(1, page), Math.Clamp(pageSize, 1, 100)), ct);
            return Results.Ok(result);
        })
        .WithName("GetIncomeExpenseList")
        .WithSummary("Gelir/gider listesi (sayfalanmış)")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/finance/income-expense-summary — gelir/gider özeti
        group.MapGet("/income-expense-summary", async (
            Guid tenantId, DateTime? from, DateTime? to,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetIncomeExpenseSummaryQuery(tenantId, from, to), ct);
            return Results.Ok(result);
        })
        .WithName("GetIncomeExpenseSummary")
        .WithSummary("Gelir/gider özet raporu")
        .Produces(200)
        .CacheOutput("Report120s");

        // GET /api/v1/finance/kar-zarar — kâr/zarar raporu
        group.MapGet("/kar-zarar", async (
            DateTime from, DateTime to, Guid? tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetKarZararQuery(from, to, tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetKarZarar")
        .WithSummary("Kâr/zarar raporu (tarih aralığı)")
        .Produces(200)
        .CacheOutput("Report120s");

        // GET /api/v1/finance/bank-accounts — banka hesap listesi
        group.MapGet("/bank-accounts", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetBankAccountsQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetBankAccounts")
        .WithSummary("Banka hesap listesi — bakiye ve detaylar")
        .Produces(200)
        .CacheOutput("Lookup60s");
    }
}
