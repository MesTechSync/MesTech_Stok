using MediatR;
using MesTech.Application.Features.Accounting.Commands.CreateAccountingExpense;
using MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;
using MesTech.Application.Features.Accounting.Commands.RunReconciliation;
using MesTech.Application.Features.Accounting.Queries.GetAccountingExpenses;
using MesTech.Application.Features.Accounting.Queries.GetBalanceSheet;
using MesTech.Application.Features.Accounting.Queries.GetBankTransactions;
using MesTech.Application.Features.Accounting.Queries.GetJournalEntries;
using MesTech.Application.Features.Accounting.Queries.GetProfitReport;
using MesTech.Application.Features.Accounting.Queries.GetReconciliationDashboard;
using MesTech.Application.Features.Accounting.Queries.GetSettlementBatches;
using MesTech.Application.Features.Accounting.Queries.GetTrialBalance;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.WebApi.Endpoints;

public static class AccountingEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/accounting")
            .WithTags("Accounting")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/accounting/trial-balance — mizan raporu
        group.MapGet("/trial-balance", async (
            Guid tenantId, DateTime startDate, DateTime endDate,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetTrialBalanceQuery(tenantId, startDate, endDate), ct);
            return Results.Ok(result);
        })
        .WithName("GetTrialBalance")
        .WithSummary("Mizan raporu (belirli tarih aralığı)");

        // GET /api/v1/accounting/balance-sheet — bilanço raporu
        group.MapGet("/balance-sheet", async (
            Guid tenantId, DateTime asOfDate,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetBalanceSheetQuery(tenantId, asOfDate), ct);
            return Results.Ok(result);
        })
        .WithName("GetBalanceSheet")
        .WithSummary("Bilanço raporu (belirli tarih itibariyle)");

        // GET /api/v1/accounting/profit-report — kâr raporu
        group.MapGet("/profit-report", async (
            Guid tenantId, string period, string? platform,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetProfitReportQuery(tenantId, period, platform), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetProfitReport")
        .WithSummary("Kâr raporu (dönem ve platform bazlı)");

        // GET /api/v1/accounting/journal-entries — yevmiye kayıtları listesi
        group.MapGet("/journal-entries", async (
            Guid tenantId, DateTime from, DateTime to,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetJournalEntriesQuery(tenantId, from, to), ct);
            return Results.Ok(result);
        })
        .WithName("GetJournalEntries")
        .WithSummary("Yevmiye kayıtları listesi (tarih aralığı)");

        // POST /api/v1/accounting/journal-entries — yeni yevmiye kaydı oluştur
        group.MapPost("/journal-entries", async (
            CreateJournalEntryCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/accounting/journal-entries/{id}", new { id });
        })
        .WithName("CreateJournalEntry")
        .WithSummary("Yeni yevmiye kaydı oluştur");

        // GET /api/v1/accounting/expenses — masraf listesi
        group.MapGet("/expenses", async (
            Guid tenantId, DateTime from, DateTime to, ExpenseSource? source,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetAccountingExpensesQuery(tenantId, from, to, source), ct);
            return Results.Ok(result);
        })
        .WithName("GetAccountingExpenses")
        .WithSummary("Masraf listesi (tarih aralığı + kaynak filtresi)");

        // POST /api/v1/accounting/expenses — yeni masraf kaydı oluştur
        group.MapPost("/expenses", async (
            CreateAccountingExpenseCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/accounting/expenses/{id}", new { id });
        })
        .WithName("CreateAccountingExpense")
        .WithSummary("Yeni masraf kaydı oluştur");

        // GET /api/v1/accounting/settlements — hakediş partileri
        group.MapGet("/settlements", async (
            Guid tenantId, DateTime? from, DateTime? to, string? platform,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetSettlementBatchesQuery(tenantId, from, to, platform), ct);
            return Results.Ok(result);
        })
        .WithName("GetSettlementBatches")
        .WithSummary("Hakediş partileri (tarih + platform filtresi)");

        // GET /api/v1/accounting/reconciliation/dashboard — mutabakat özet panosu
        group.MapGet("/reconciliation/dashboard", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetReconciliationDashboardQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetReconciliationDashboard")
        .WithSummary("Mutabakat dashboard — eşleştirme durum özeti");

        // POST /api/v1/accounting/reconciliation/run — otomatik mutabakat çalıştır
        group.MapPost("/reconciliation/run", async (
            RunReconciliationCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("RunReconciliation")
        .WithSummary("Otomatik mutabakat eşleştirme çalıştır");

        // GET /api/v1/accounting/bank-transactions — banka hareketleri
        group.MapGet("/bank-transactions", async (
            Guid tenantId, Guid bankAccountId, DateTime? from, DateTime? to,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetBankTransactionsQuery(tenantId, bankAccountId, from, to), ct);
            return Results.Ok(result);
        })
        .WithName("GetBankTransactions")
        .WithSummary("Banka hareketleri (hesap + tarih filtresi)");
    }
}
