using MesTech.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using MesTech.Application.Features.Accounting.Queries.GetExpenseReport;
using MesTech.Application.Features.Accounting.Queries.GetKdvReport;
using MesTech.Application.Features.Accounting.Queries.GetMonthlySummary;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
using MesTech.Application.Features.Calendar.Commands.GenerateTaxCalendar;
using MesTech.Application.Features.Finance.Queries.GetProfitLoss;
using MesTech.Application.Features.Reports.PlatformSalesReport;
using MesTech.Application.Features.Reports.CargoPerformanceReport;
using MesTech.Application.Features.Reports.CustomerLifetimeValueReport;
using MesTech.Application.Features.Reports.CustomerSegmentReport;
using MesTech.Application.Features.Reports.InventoryValuationReport;
using MesTech.Application.Features.Reports.OrderFulfillmentReport;
using MesTech.Application.Features.Reports.ProfitabilityReport;
using MesTech.Application.Features.Reports.SalesAnalytics;
using MesTech.Application.Features.Reports.StockTurnoverReport;
using MesTech.Application.Features.Reports.CommissionReport;
using MesTech.Application.Features.Reports.ErpReconciliationReport;
using MesTech.Application.Features.Reports.FulfillmentCostReport;
using MesTech.Application.Features.Reports.PlatformPerformanceReport;
using MesTech.Application.Features.Reports.TaxSummaryReport;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.WebApi.Endpoints;

public static class ReportEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/reports")
            .WithTags("Reports")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/reports/profit-loss — aylik kar/zarar raporu
        group.MapGet("/profit-loss", async (
            Guid tenantId, int year, int month,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetProfitLossQuery(tenantId, year, month), ct);
            return Results.Ok(result);
        })
        .WithName("GetProfitLossReport")
        .WithSummary("Aylik kar/zarar raporu")
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Report120s");

        // GET /api/v1/reports/monthly-summary/{year}/{month} — aylik ozet rapor
        group.MapGet("/monthly-summary/{year:int}/{month:int}", async (
            int year, int month, Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            if (month < 1 || month > 12)
                return Results.Problem(detail: "Month must be between 1 and 12.", statusCode: 400);
            if (year < 2000 || year > 2100)
                return Results.Problem(detail: "Year must be between 2000 and 2100.", statusCode: 400);

            var result = await mediator.Send(
                new GetMonthlySummaryQuery(year, month, tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetMonthlySummary")
        .WithSummary("Aylik ozet raporu (satis, komisyon, gider, vergi metrikleri)")
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Report120s");

        // GET /api/v1/reports/kdv/{year}/{month} — KDV raporu
        group.MapGet("/kdv/{year:int}/{month:int}", async (
            int year, int month, Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            if (month < 1 || month > 12)
                return Results.Problem(detail: "Month must be between 1 and 12.", statusCode: 400);
            if (year < 2000 || year > 2100)
                return Results.Problem(detail: "Year must be between 2000 and 2100.", statusCode: 400);

            var result = await mediator.Send(
                new GetKdvReportQuery(tenantId, year, month), ct);
            return Results.Ok(result);
        })
        .WithName("GetKdvReport")
        .WithSummary("KDV raporu (hesaplanan, indirilecek, odenecek KDV)")
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Report120s");

        // POST /api/v1/reports/generate-tax-calendar/{year} — vergi takvimi olustur (legacy compat)
        // Primary endpoint: POST /api/v1/calendar/generate-tax-calendar/{year}
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
        .WithName("GenerateTaxCalendarFromReports")
        .WithSummary("Vergi takvimi olustur (yillik ~40 etkinlik)").Produces(200).Produces(400).ProducesProblem(401).ProducesProblem(429);

        // GET /api/v1/reports/platform-comparison — platform bazli satis karsilastirmasi
        group.MapGet("/platform-comparison", async (
            Guid tenantId, DateTime startDate, DateTime endDate, string? platform,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new PlatformSalesReportQuery(tenantId, startDate, endDate, platform), ct);
            return Results.Ok(result);
        })
        .WithName("GetPlatformComparison")
        .WithSummary("Platform bazli satis karsilastirma raporu (tarih araligi + platform filtresi)")
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Report120s");

        // GET /api/v1/reports/profitability — karlilik raporu (Net Kar formulu)
        group.MapGet("/profitability", async (
            Guid tenantId, DateTime from, DateTime to,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ProfitabilityReportQuery(tenantId, from, to), ct);
            return Results.Ok(result);
        })
        .WithName("GetProfitabilityReport")
        .WithSummary("Karlilik raporu — Net Kar = Gelir - Alis - Komisyon - Kargo - KDV")
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Report120s");

        // ─── DEFTER KAPATMA: 7 eksik rapor endpoint [ENT-DEV6] ───

        // GET /api/v1/reports/cargo-performance — kargo performans raporu
        group.MapGet("/cargo-performance", async (
            Guid tenantId, DateTime startDate, DateTime endDate,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new CargoPerformanceReportQuery(tenantId, startDate, endDate), ct);
            return Results.Ok(result);
        })
        .WithName("GetCargoPerformanceReport")
        .WithSummary("Kargo performans raporu (firma bazlı teslimat süresi + maliyet)")
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Report120s");

        // GET /api/v1/reports/customer-lifetime-value — müşteri yaşam boyu değeri
        group.MapGet("/customer-lifetime-value", async (
            Guid tenantId, DateTime startDate, DateTime endDate, int minOrderCount = 1,
            ISender mediator = default!, CancellationToken ct = default) =>
        {
            var result = await mediator.Send(
                new CustomerLifetimeValueReportQuery(tenantId, startDate, endDate, minOrderCount), ct);
            return Results.Ok(result);
        })
        .WithName("GetCustomerLifetimeValueReport")
        .WithSummary("Müşteri yaşam boyu değeri raporu (CLV)")
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Report120s");

        // GET /api/v1/reports/customer-segments — müşteri segment raporu
        group.MapGet("/customer-segments", async (
            Guid tenantId, DateTime startDate, DateTime endDate,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new CustomerSegmentReportQuery(tenantId, startDate, endDate), ct);
            return Results.Ok(result);
        })
        .WithName("GetCustomerSegmentReport")
        .WithSummary("Müşteri segment analizi (RFM bazlı)")
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Report120s");

        // GET /api/v1/reports/inventory-valuation — envanter değerleme raporu
        group.MapGet("/inventory-valuation", async (
            Guid tenantId, Guid? categoryFilter,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new InventoryValuationReportQuery(tenantId, categoryFilter), ct);
            return Results.Ok(result);
        })
        .WithName("GetInventoryValuationReport")
        .WithSummary("Envanter değerleme raporu (kategori filtresi)")
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Report120s");

        // GET /api/v1/reports/order-fulfillment — sipariş karşılama raporu
        group.MapGet("/order-fulfillment", async (
            Guid tenantId, DateTime startDate, DateTime endDate,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new OrderFulfillmentReportQuery(tenantId, startDate, endDate), ct);
            return Results.Ok(result);
        })
        .WithName("GetOrderFulfillmentReport")
        .WithSummary("Sipariş karşılama performans raporu")
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Report120s");

        // GET /api/v1/reports/stock-turnover — stok devir hızı raporu
        group.MapGet("/stock-turnover", async (
            Guid tenantId, DateTime startDate, DateTime endDate, Guid? categoryFilter,
            ISender mediator = default!, CancellationToken ct = default) =>
        {
            var result = await mediator.Send(
                new StockTurnoverReportQuery(tenantId, startDate, endDate, categoryFilter), ct);
            return Results.Ok(result);
        })
        .WithName("GetStockTurnoverReport")
        .WithSummary("Stok devir hızı raporu (kategori filtresi)")
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Report120s");

        // GET /api/v1/reports/tax-summary — vergi özet raporu
        group.MapGet("/tax-summary", async (
            Guid tenantId, DateTime startDate, DateTime endDate,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new TaxSummaryReportQuery(tenantId, startDate, endDate), ct);
            return Results.Ok(result);
        })
        .WithName("GetTaxSummaryReport")
        .WithSummary("Vergi özet raporu (KDV, gelir vergisi, stopaj)")
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Report120s");

        // ─── V5 YENİ RAPOR ENDPOINT'LERİ [ENT-DEV6] ───

        // GET /api/v1/reports/commission — platform bazlı komisyon raporu
        group.MapGet("/commission", async (
            Guid tenantId, DateTime startDate, DateTime endDate,
            PlatformType? platform = null,
            ISender mediator = default!, CancellationToken ct = default) =>
        {
            var result = await mediator.Send(
                new CommissionReportQuery(tenantId, startDate, endDate, platform), ct);
            return Results.Ok(result);
        })
        .WithName("GetCommissionReport")
        .WithSummary("Platform bazlı komisyon raporu — dönem karşılaştırmalı")
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Report120s");

        // GET /api/v1/reports/fulfillment-cost — fulfillment maliyet raporu
        group.MapGet("/fulfillment-cost", async (
            Guid tenantId, DateTime startDate, DateTime endDate,
            int? center = null,
            ISender mediator = default!, CancellationToken ct = default) =>
        {
            var centerFilter = center.HasValue
                ? (MesTech.Application.DTOs.Fulfillment.FulfillmentCenter?)center.Value
                : null;
            var result = await mediator.Send(
                new FulfillmentCostReportQuery(tenantId, startDate, endDate, centerFilter), ct);
            return Results.Ok(result);
        })
        .WithName("GetFulfillmentCostReport")
        .WithSummary("FBA + Hepsilojistik maliyet analizi raporu")
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Report120s");

        // GET /api/v1/reports/erp-reconciliation — ERP cari mutabakat raporu
        group.MapGet("/erp-reconciliation", async (
            Guid tenantId, ErpProvider erpProvider,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ErpReconciliationReportQuery(tenantId, erpProvider), ct);
            return Results.Ok(result);
        })
        .WithName("GetErpReconciliationReport")
        .WithSummary("ERP cari hesap mutabakat raporu (MesTech vs ERP eşleştirme)")
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Report120s");

        // GET /api/v1/reports/platform-performance — platform performans raporu
        group.MapGet("/platform-performance", async (
            Guid tenantId, DateTime startDate, DateTime endDate,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new PlatformPerformanceReportQuery(tenantId, startDate, endDate), ct);
            return Results.Ok(result);
        })
        .WithName("GetPlatformPerformanceReport")
        .WithSummary("Platform performans raporu — sipariş, gelir, iade oranı, skor")
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Report120s");

        // ─── V5 EXPORT ENDPOINT'LERİ [ENT-DEV6] ───

        // GET /api/v1/reports/commission/export — komisyon raporu export (PDF/Excel/CSV)
        group.MapGet("/commission/export", async (
            Guid tenantId, DateTime startDate, DateTime endDate,
            string format = "pdf",
            PlatformType? platform = null,
            ISender mediator = default!,
            IReportExportService exportService = default!,
            CancellationToken ct = default) =>
        {
            var report = await mediator.Send(
                new CommissionReportQuery(tenantId, startDate, endDate, platform), ct);
            return await ExportResult(exportService, report.PlatformBreakdown, "Komisyon Raporu", format, ct);
        })
        .WithName("ExportCommissionReport")
        .WithSummary("Komisyon raporu export — PDF, Excel veya CSV").Produces(200).Produces(400).ProducesProblem(401).ProducesProblem(429)
        .WithRequestTimeout("LongRunning");

        // GET /api/v1/reports/platform-performance/export — platform performans export
        group.MapGet("/platform-performance/export", async (
            Guid tenantId, DateTime startDate, DateTime endDate,
            string format = "pdf",
            ISender mediator = default!,
            IReportExportService exportService = default!,
            CancellationToken ct = default) =>
        {
            var report = await mediator.Send(
                new PlatformPerformanceReportQuery(tenantId, startDate, endDate), ct);
            return await ExportResult(exportService, report.Platforms, "Platform Performans Raporu", format, ct);
        })
        .WithName("ExportPlatformPerformanceReport")
        .WithSummary("Platform performans raporu export — PDF, Excel veya CSV").Produces(200).Produces(400).ProducesProblem(401).ProducesProblem(429)
        .WithRequestTimeout("LongRunning");

        // GET /api/v1/reports/profitability/export — kârlılık raporu export
        group.MapGet("/profitability/export", async (
            Guid tenantId, DateTime from, DateTime to,
            string format = "pdf",
            ISender mediator = default!,
            IReportExportService exportService = default!,
            CancellationToken ct = default) =>
        {
            var report = await mediator.Send(
                new ProfitabilityReportQuery(tenantId, from, to), ct);
            return await ExportResult(exportService, report.ByPlatform, "Kârlılık Raporu", format, ct);
        })
        .WithName("ExportProfitabilityReport")
        .WithSummary("Kârlılık raporu export — PDF, Excel veya CSV").Produces(200).Produces(400).ProducesProblem(401).ProducesProblem(429)
        .WithRequestTimeout("LongRunning");

        // ─── V6 EXPORT ENDPOINT'LERİ [ENT-DEV6] ───

        // GET /api/v1/reports/fulfillment-cost/export — fulfillment maliyet export
        group.MapGet("/fulfillment-cost/export", async (
            Guid tenantId, DateTime startDate, DateTime endDate,
            string format = "pdf",
            int? center = null,
            ISender mediator = default!,
            IReportExportService exportService = default!,
            CancellationToken ct = default) =>
        {
            var centerFilter = center.HasValue
                ? (MesTech.Application.DTOs.Fulfillment.FulfillmentCenter?)center.Value
                : null;
            var report = await mediator.Send(
                new FulfillmentCostReportQuery(tenantId, startDate, endDate, centerFilter), ct);
            return await ExportResult(exportService, report.Centers, "Fulfillment Maliyet Raporu", format, ct);
        })
        .WithName("ExportFulfillmentCostReport")
        .WithSummary("Fulfillment maliyet raporu export — PDF, Excel veya CSV").Produces(200).Produces(400).ProducesProblem(401).ProducesProblem(429)
        .WithRequestTimeout("LongRunning");

        // GET /api/v1/reports/cargo-performance/export — kargo performans export
        group.MapGet("/cargo-performance/export", async (
            Guid tenantId, DateTime startDate, DateTime endDate,
            string format = "pdf",
            ISender mediator = default!,
            IReportExportService exportService = default!,
            CancellationToken ct = default) =>
        {
            var report = await mediator.Send(
                new CargoPerformanceReportQuery(tenantId, startDate, endDate), ct);
            return await ExportResult(exportService, report, "Kargo Performans Raporu", format, ct);
        })
        .WithName("ExportCargoPerformanceReport")
        .WithSummary("Kargo performans raporu export — PDF, Excel veya CSV").Produces(200).Produces(400).ProducesProblem(401).ProducesProblem(429)
        .WithRequestTimeout("LongRunning");

        // GET /api/v1/reports/inventory-valuation/export — envanter değerleme export
        group.MapGet("/inventory-valuation/export", async (
            Guid tenantId, string format = "pdf",
            Guid? categoryFilter = null,
            ISender mediator = default!,
            IReportExportService exportService = default!,
            CancellationToken ct = default) =>
        {
            var report = await mediator.Send(
                new InventoryValuationReportQuery(tenantId, categoryFilter), ct);
            return await ExportResult(exportService, report, "Envanter Degerleme Raporu", format, ct);
        })
        .WithName("ExportInventoryValuationReport")
        .WithSummary("Envanter değerleme raporu export — PDF, Excel veya CSV").Produces(200).Produces(400).ProducesProblem(401).ProducesProblem(429)
        .WithRequestTimeout("LongRunning");

        // GET /api/v1/reports/stock-turnover/export — stok devir hızı export
        group.MapGet("/stock-turnover/export", async (
            Guid tenantId, DateTime startDate, DateTime endDate,
            string format = "pdf",
            Guid? categoryFilter = null,
            ISender mediator = default!,
            IReportExportService exportService = default!,
            CancellationToken ct = default) =>
        {
            var report = await mediator.Send(
                new StockTurnoverReportQuery(tenantId, startDate, endDate, categoryFilter), ct);
            return await ExportResult(exportService, report, "Stok Devir Hizi Raporu", format, ct);
        })
        .WithName("ExportStockTurnoverReport")
        .WithSummary("Stok devir hızı raporu export — PDF, Excel veya CSV").Produces(200).Produces(400).ProducesProblem(401).ProducesProblem(429)
        .WithRequestTimeout("LongRunning");

        // GET /api/v1/reports/erp-reconciliation/export — ERP mutabakat export
        group.MapGet("/erp-reconciliation/export", async (
            Guid tenantId, ErpProvider erpProvider,
            string format = "pdf",
            ISender mediator = default!,
            IReportExportService exportService = default!,
            CancellationToken ct = default) =>
        {
            var report = await mediator.Send(
                new ErpReconciliationReportQuery(tenantId, erpProvider), ct);
            return await ExportResult(exportService, report.UnmatchedItems, "ERP Mutabakat Raporu", format, ct);
        })
        .WithName("ExportErpReconciliationReport")
        .WithSummary("ERP mutabakat raporu export — PDF, Excel veya CSV").Produces(200).Produces(400).ProducesProblem(401).ProducesProblem(429)
        .WithRequestTimeout("LongRunning");

        // GET /api/v1/reports/sales-analytics — satış analiz raporu
        group.MapGet("/sales-analytics", async (
            Guid tenantId, DateTime from, DateTime to,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetSalesAnalyticsQuery(tenantId, from, to), ct);
            return Results.Ok(result);
        })
        .WithName("GetSalesAnalytics")
        .WithSummary("Satış analiz raporu — platform bazlı gelir, adet, trend")
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Report120s");

        // G564 endpoints
        MapG564Endpoints(group);
    }

    private static async Task<IResult> ExportResult<T>(
        IReportExportService exportService,
        IEnumerable<T> data,
        string title,
        string format,
        CancellationToken ct)
    {
        var fmt = format.ToLowerInvariant();
        return fmt switch
        {
            "pdf" => Results.File(
                await exportService.ExportToPdfAsync(data, title, ct),
                "application/pdf",
                $"{title.Replace(' ', '_')}_{DateTime.UtcNow:yyyyMMdd}.pdf"),
            "xlsx" or "excel" => Results.File(
                await exportService.ExportToExcelAsync(data, title, ct),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{title.Replace(' ', '_')}_{DateTime.UtcNow:yyyyMMdd}.xlsx"),
            "csv" => Results.File(
                await exportService.ExportToCsvAsync(data, ct),
                "text/csv",
                $"{title.Replace(' ', '_')}_{DateTime.UtcNow:yyyyMMdd}.csv"),
            _ => Results.Problem(detail: "Desteklenen formatlar: pdf, xlsx, csv", statusCode: 400)
        };
    }

    // ── G564 ENDPOINT'LER ──

    // GET /api/v1/reports/expenses — gider raporu (G564)
    private static void MapG564Endpoints(RouteGroupBuilder group)
    {
        group.MapGet("/expenses", async (
            Guid tenantId,
            DateTime from,
            DateTime to,
            string? category,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetExpenseReportQuery(tenantId, from, to, category), ct);
            return Results.Ok(result);
        })
        .WithName("GetExpenseReport")
        .WithSummary("Gider raporu — tarih aralığı + kategori filtresi")
        .Produces<ExpenseReportDto>(200);

        // POST /api/v1/reports/export — genel rapor dışa aktarım (G564)
        group.MapPost("/export", async (
            ExportReportCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            if (result.FileData.Length == 0)
                return Results.Problem(detail: "Report export produced no data", statusCode: 400);
            return Results.File(result.FileData.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                result.FileName);
        })
        .WithName("ExportReport")
        .WithSummary("Genel rapor dışa aktar — rapor tipi + format + parametreler")
        .Produces(200).Produces(400).ProducesProblem(401).ProducesProblem(429);
    }
}
