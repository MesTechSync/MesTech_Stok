using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Commands.DeleteExpense;
using MesTech.Application.Commands.UpdateExpense;
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
using MesTech.Application.Features.Accounting.Commands.CreateChartOfAccount;
using MesTech.Application.Features.Accounting.Commands.DeleteChartOfAccount;
using MesTech.Application.Features.Accounting.Commands.UpdateChartOfAccount;
using MesTech.Application.Features.Accounting.Queries.GetCashFlowTrend;
using MesTech.Application.Features.Accounting.Queries.GetChartOfAccounts;
using MesTech.Application.Features.Accounting.Queries.GetPlatformCommissionRates;
using MesTech.Application.Features.Accounting.Commands.CreatePlatformCommissionRate;
using MesTech.Application.Features.Accounting.Commands.UpdatePlatformCommissionRate;
using MesTech.Application.Features.Accounting.Commands.ApproveReconciliation;
using MesTech.Application.Features.Accounting.Commands.CreateAccountingBankAccount;
using MesTech.Application.Features.Accounting.Commands.CreateCounterparty;
using MesTech.Application.Features.Accounting.Commands.UpdateCounterparty;
using MesTech.Application.Features.Accounting.Commands.CreateFinancialGoal;
using MesTech.Application.Features.Accounting.Commands.ImportBankStatement;
using MesTech.Application.Features.Accounting.Commands.ImportSettlement;
using MesTech.Application.Features.Accounting.Commands.RecordCargoExpense;
using MesTech.Application.Features.Accounting.Commands.RecordCommission;
using MesTech.Application.Features.Accounting.Commands.RejectReconciliation;
using MesTech.Application.Features.Accounting.Commands.UploadAccountingDocument;
using MesTech.Application.Features.Accounting.Queries.GetAccountBalance;
using MesTech.Application.Features.Accounting.Queries.GetCargoComparison;
using MesTech.Application.Features.Accounting.Queries.GetCashFlowReport;
using MesTech.Application.Features.Accounting.Queries.GetCommissionSummary;
using MesTech.Application.Features.Accounting.Queries.GetCounterparties;
using MesTech.Application.Features.Accounting.Queries.GetFifoCOGS;
using MesTech.Application.Features.Accounting.Queries.GetKdvDeclarationDraft;
using MesTech.Application.Features.Accounting.Queries.GetPendingReviews;
using MesTech.Application.Features.Accounting.Queries.GetReconciliationMatches;
using MesTech.Application.Features.Accounting.Queries.GetTaxSummary;
using MesTech.Application.Features.Accounting.Queries.ValidateBalanceSheet;
using MesTech.Application.Features.Accounting.Queries.ValidateTrialBalance;
using MesTech.Application.Queries.GetExpenseById;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Enums;

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
            return Results.Created($"/api/v1/accounting/journal-entries/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
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
            return Results.Created($"/api/v1/accounting/expenses/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("CreateAccountingExpense")
        .WithSummary("Yeni masraf kaydı oluştur");

        // GET /api/v1/accounting/expenses/{id} — tek masraf kaydi
        group.MapGet("/expenses/{id:guid}", async (
            Guid id, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetExpenseByIdQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetExpenseById")
        .WithSummary("Tek masraf kaydi detayi");

        // PUT /api/v1/accounting/expenses/{id} — masraf kaydi guncelle
        group.MapPut("/expenses/{id:guid}", async (
            Guid id, UpdateExpenseCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var updated = command with { Id = id };
            await mediator.Send(updated, ct);
            return Results.NoContent();
        })
        .WithName("UpdateExpense")
        .WithSummary("Masraf kaydi guncelle");

        // DELETE /api/v1/accounting/expenses/{id} — masraf kaydi sil (soft delete)
        group.MapDelete("/expenses/{id:guid}", async (
            Guid id, ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteExpenseCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("DeleteExpense")
        .WithSummary("Masraf kaydini sil (soft delete)");

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

        // GET /api/v1/accounting/chart-of-accounts — hesap planı listesi
        group.MapGet("/chart-of-accounts", async (
            Guid tenantId, bool? isActive,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetChartOfAccountsQuery(tenantId, isActive ?? true), ct);
            return Results.Ok(result);
        })
        .WithName("GetChartOfAccounts")
        .WithSummary("Hesap planı listesi (aktif/pasif filtresi)");

        // GET /api/v1/accounting/commission-rates — platform komisyon oranları
        group.MapGet("/commission-rates", async (
            Guid tenantId, PlatformType? platformType, bool? isActive,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetPlatformCommissionRatesQuery(tenantId, platformType, isActive ?? true), ct);
            return Results.Ok(result);
        })
        .WithName("GetPlatformCommissionRates")
        .WithSummary("Platform komisyon oranları listesi (platform + aktif filtresi)");

        // POST /api/v1/accounting/commission-rates — yeni komisyon oranı oluştur (Dalga 14 M2)
        group.MapPost("/commission-rates", async (
            CreatePlatformCommissionRateCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/accounting/commission-rates/{result}", result);
        })
        .WithName("CreatePlatformCommissionRate")
        .WithSummary("Yeni platform komisyon oranı oluştur");

        // PUT /api/v1/accounting/commission-rates/{id} — komisyon oranı güncelle (Dalga 14 M2)
        group.MapPut("/commission-rates/{id:guid}", async (
            Guid id, UpdatePlatformCommissionRateCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var updated = command with { Id = id };
            await mediator.Send(updated, ct);
            return Results.NoContent();
        })
        .WithName("UpdatePlatformCommissionRate")
        .WithSummary("Platform komisyon oranı güncelle");

        // GET /api/v1/accounting/shipment-costs — kargo maliyet listesi
        group.MapGet("/shipment-costs", async (
            Guid tenantId,
            DateTime? from, DateTime? to,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new MesTech.Application.Features.Accounting.Queries.GetShipmentCosts.GetShipmentCostsQuery(
                    tenantId, from, to), ct);
            return Results.Ok(result);
        })
        .WithName("GetShipmentCosts")
        .WithSummary("Kargo maliyet listesi");

        // GET /api/v1/accounting/periods — muhasebe dönemleri
        group.MapGet("/periods", async (
            Guid tenantId,
            int? year,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new MesTech.Application.Features.Accounting.Queries.GetAccountingPeriods.GetAccountingPeriodsQuery(
                    tenantId, year), ct);
            return Results.Ok(result);
        })
        .WithName("GetAccountingPeriods")
        .WithSummary("Muhasebe dönemleri listesi");

        // POST /api/v1/accounting/periods/close — dönem kapat
        group.MapPost("/periods/close", async (
            MesTech.Application.Features.Accounting.Commands.CloseAccountingPeriod.CloseAccountingPeriodCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Ok(ApiResponse<StatusResponse>.Ok(new StatusResponse("closed")));
        })
        .WithName("CloseAccountingPeriod")
        .WithSummary("Muhasebe dönemini kapat");

        // ─── DEFTER KAPATMA: Eksik 21 endpoint eklendi [ENT-DEV6] ───

        // POST /api/v1/accounting/reconciliation/approve — mutabakat onayla
        group.MapPost("/reconciliation/approve", async (
            ApproveReconciliationCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(command, ct);
            return Results.NoContent();
        })
        .WithName("ApproveReconciliation")
        .WithSummary("Mutabakat eşleşmesini onayla");

        // POST /api/v1/accounting/reconciliation/reject — mutabakat reddet
        group.MapPost("/reconciliation/reject", async (
            RejectReconciliationCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(command, ct);
            return Results.NoContent();
        })
        .WithName("RejectReconciliation")
        .WithSummary("Mutabakat eşleşmesini reddet");

        // GET /api/v1/accounting/reconciliation/matches — mutabakat eşleşmeleri
        group.MapGet("/reconciliation/matches", async (
            Guid tenantId, int? status,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetReconciliationMatchesQuery(tenantId, status.HasValue ? (ReconciliationStatus)status.Value : null), ct);
            return Results.Ok(result);
        })
        .WithName("GetReconciliationMatches")
        .WithSummary("Mutabakat eşleşme listesi (durum filtresi)");

        // GET /api/v1/accounting/reconciliation/pending-reviews — bekleyen incelemeler
        group.MapGet("/reconciliation/pending-reviews", async (
            Guid tenantId, int page = 1, int pageSize = 20,
            ISender mediator = default!, CancellationToken ct = default) =>
        {
            var result = await mediator.Send(
                new GetPendingReviewsQuery(tenantId, pageSize, page), ct);
            return Results.Ok(result);
        })
        .WithName("GetPendingReviews")
        .WithSummary("Bekleyen mutabakat incelemeleri");

        // POST /api/v1/accounting/bank-accounts — banka hesabı oluştur
        group.MapPost("/bank-accounts", async (
            CreateAccountingBankAccountCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/accounting/bank-accounts/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("CreateAccountingBankAccount")
        .WithSummary("Yeni banka hesabı tanımla");

        // GET /api/v1/accounting/account-balance — hesap bakiyesi
        group.MapGet("/account-balance", async (
            Guid tenantId, Guid accountId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetAccountBalanceQuery(tenantId, accountId), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetAccountBalance")
        .WithSummary("Hesap bakiyesi sorgula");

        // POST /api/v1/accounting/financial-goals — mali hedef oluştur
        group.MapPost("/financial-goals", async (
            CreateFinancialGoalCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/accounting/financial-goals/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("CreateFinancialGoal")
        .WithSummary("Mali hedef oluştur");

        // POST /api/v1/accounting/bank-statements/import — banka ekstresi içe aktar
        group.MapPost("/bank-statements/import", async (
            ImportBankStatementCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var count = await mediator.Send(command, ct);
            return Results.Ok(ApiResponse<object>.Ok(new { importedCount = count }));
        })
        .WithName("ImportBankStatement")
        .WithSummary("Banka ekstresi içe aktar");

        // POST /api/v1/accounting/settlements/import — hakediş içe aktar
        group.MapPost("/settlements/import", async (
            ImportSettlementCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/accounting/settlements/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("ImportSettlement")
        .WithSummary("Hakediş dosyası içe aktar");

        // POST /api/v1/accounting/cargo-expenses — kargo gideri kaydet
        group.MapPost("/cargo-expenses", async (
            RecordCargoExpenseCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/accounting/cargo-expenses/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("RecordCargoExpense")
        .WithSummary("Kargo gideri kaydı oluştur");

        // POST /api/v1/accounting/commissions — komisyon kaydı oluştur
        group.MapPost("/commissions", async (
            RecordCommissionCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/accounting/commissions/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("RecordCommission")
        .WithSummary("Platform komisyon kaydı oluştur");

        // POST /api/v1/accounting/documents/upload — muhasebe belgesi yükle
        group.MapPost("/documents/upload", async (
            UploadAccountingDocumentCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/accounting/documents/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("UploadAccountingDocument")
        .WithSummary("Muhasebe belgesi yükle");

        // GET /api/v1/accounting/cash-flow — nakit akış raporu
        group.MapGet("/cash-flow", async (
            Guid tenantId, DateTime from, DateTime to,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetCashFlowReportQuery(tenantId, from, to), ct);
            return Results.Ok(result);
        })
        .WithName("GetCashFlowReport")
        .WithSummary("Nakit akış raporu (tarih aralığı)");

        // GET /api/v1/accounting/commission-summary — komisyon özet raporu
        group.MapGet("/commission-summary", async (
            Guid tenantId, DateTime from, DateTime to,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetCommissionSummaryQuery(tenantId, from, to), ct);
            return Results.Ok(result);
        })
        .WithName("GetCommissionSummary")
        .WithSummary("Komisyon özet raporu (tarih aralığı)");

        // GET /api/v1/accounting/counterparties — cari hesap listesi (detaylı)
        group.MapGet("/counterparties", async (
            Guid tenantId, int? type, bool? isActive,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetCounterpartiesQuery(tenantId,
                    type.HasValue ? (CounterpartyType)type.Value : null,
                    isActive ?? true), ct);
            return Results.Ok(result);
        })
        .WithName("GetCounterparties")
        .WithSummary("Cari hesap listesi (tip + aktif filtresi)");

        // GET /api/v1/accounting/fifo-cogs — FIFO maliyet hesabı
        group.MapGet("/fifo-cogs", async (
            Guid tenantId, Guid? productId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetFifoCOGSQuery(tenantId, productId), ct);
            return Results.Ok(result);
        })
        .WithName("GetFifoCOGS")
        .WithSummary("FIFO satılan malın maliyeti hesabı");

        // GET /api/v1/accounting/kdv-declaration-draft — KDV beyanname taslağı
        group.MapGet("/kdv-declaration-draft", async (
            Guid tenantId, string period,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetKdvDeclarationDraftQuery(tenantId, period), ct);
            return Results.Ok(result);
        })
        .WithName("GetKdvDeclarationDraft")
        .WithSummary("KDV beyannamesi taslağı (dönem: 2026-03 formatında)");

        // GET /api/v1/accounting/tax-summary — vergi özet raporu
        group.MapGet("/tax-summary", async (
            Guid tenantId, string period,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetTaxSummaryQuery(tenantId, period), ct);
            return Results.Ok(result);
        })
        .WithName("GetTaxSummary")
        .WithSummary("Vergi özet raporu (dönem bazlı)");

        // POST /api/v1/accounting/cargo-comparison — kargo karşılaştırma
        group.MapPost("/cargo-comparison", async (
            GetCargoComparisonQuery query,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetCargoComparison")
        .WithSummary("Kargo firma karşılaştırma (fiyat/süre)");

        // GET /api/v1/accounting/validate-balance-sheet — bilanço doğrulama
        group.MapGet("/validate-balance-sheet", async (
            Guid tenantId, DateTime asOfDate,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ValidateBalanceSheetQuery(tenantId, asOfDate), ct);
            return Results.Ok(result);
        })
        .WithName("ValidateBalanceSheet")
        .WithSummary("Bilanço doğrulama (aktif = pasif kontrolü)");

        // POST /api/v1/accounting/chart-of-accounts — hesap planı oluştur
        group.MapPost("/chart-of-accounts", async (
            CreateChartOfAccountCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/accounting/chart-of-accounts/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("CreateChartOfAccount")
        .WithSummary("Yeni hesap planı kalemi oluştur");

        // PUT /api/v1/accounting/chart-of-accounts/{id} — hesap planı güncelle
        group.MapPut("/chart-of-accounts/{id:guid}", async (
            Guid id, UpdateChartOfAccountCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var updated = command with { Id = id };
            var success = await mediator.Send(updated, ct);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("UpdateChartOfAccount")
        .WithSummary("Hesap planı kalemini güncelle");

        // DELETE /api/v1/accounting/chart-of-accounts/{id} — hesap planı sil
        group.MapDelete("/chart-of-accounts/{id:guid}", async (
            Guid id,
            ISender mediator, CancellationToken ct) =>
        {
            var success = await mediator.Send(new DeleteChartOfAccountCommand(id), ct);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteChartOfAccount")
        .WithSummary("Hesap planı kalemini sil");

        // GET /api/v1/accounting/cash-flow-trend — nakit akış trendi
        group.MapGet("/cash-flow-trend", async (
            Guid tenantId, int months,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetCashFlowTrendQuery(tenantId, months), ct);
            return Results.Ok(result);
        })
        .WithName("GetCashFlowTrend")
        .WithSummary("Nakit akış trendi (aylık gelir/gider/net)");

        // GET /api/v1/accounting/validate-trial-balance — mizan doğrulama
        group.MapGet("/validate-trial-balance", async (
            Guid tenantId, DateTime startDate, DateTime endDate,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ValidateTrialBalanceQuery(tenantId, startDate, endDate), ct);
            return Results.Ok(result);
        })
        .WithName("ValidateTrialBalance")
        .WithSummary("Mizan doğrulama (borç = alacak kontrolü)");

        // POST /api/v1/accounting/counterparties — yeni karşı taraf oluştur
        group.MapPost("/counterparties", async (
            CreateCounterpartyCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/accounting/counterparties/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("CreateCounterparty")
        .WithSummary("Yeni karşı taraf (müşteri/tedarikçi) oluştur");

        // PUT /api/v1/accounting/counterparties/{id} — karşı taraf güncelle
        group.MapPut("/counterparties/{id:guid}", async (
            Guid id, UpdateCounterpartyCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var updated = command with { Id = id };
            var success = await mediator.Send(updated, ct);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("UpdateCounterparty")
        .WithSummary("Karşı taraf bilgilerini güncelle");
    }
}
