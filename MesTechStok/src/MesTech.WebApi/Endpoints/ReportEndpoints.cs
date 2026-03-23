using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetKdvReport;
using MesTech.Application.Features.Accounting.Queries.GetMonthlySummary;
using MesTech.Application.Features.Calendar.Commands.GenerateTaxCalendar;
using MesTech.Application.Features.Finance.Queries.GetProfitLoss;
using MesTech.Application.Features.Reports.PlatformSalesReport;
using MesTech.Application.Features.Reports.ProfitabilityReport;

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
        .WithSummary("Aylik kar/zarar raporu");

        // GET /api/v1/reports/monthly-summary/{year}/{month} — aylik ozet rapor
        group.MapGet("/monthly-summary/{year:int}/{month:int}", async (
            int year, int month, Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            if (month < 1 || month > 12)
                return Results.BadRequest(new { error = "Month must be between 1 and 12." });
            if (year < 2000 || year > 2100)
                return Results.BadRequest(new { error = "Year must be between 2000 and 2100." });

            var result = await mediator.Send(
                new GetMonthlySummaryQuery(year, month, tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetMonthlySummary")
        .WithSummary("Aylik ozet raporu (satis, komisyon, gider, vergi metrikleri)");

        // GET /api/v1/reports/kdv/{year}/{month} — KDV raporu
        group.MapGet("/kdv/{year:int}/{month:int}", async (
            int year, int month, Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            if (month < 1 || month > 12)
                return Results.BadRequest(new { error = "Month must be between 1 and 12." });
            if (year < 2000 || year > 2100)
                return Results.BadRequest(new { error = "Year must be between 2000 and 2100." });

            var result = await mediator.Send(
                new GetKdvReportQuery(tenantId, year, month), ct);
            return Results.Ok(result);
        })
        .WithName("GetKdvReport")
        .WithSummary("KDV raporu (hesaplanan, indirilecek, odenecek KDV)");

        // POST /api/v1/reports/generate-tax-calendar/{year} — vergi takvimi olustur (legacy compat)
        // Primary endpoint: POST /api/v1/calendar/generate-tax-calendar/{year}
        group.MapPost("/generate-tax-calendar/{year:int}", async (
            int year, Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            if (year < 2000 || year > 2100)
                return Results.BadRequest(new { error = "Year must be between 2000 and 2100." });

            var count = await mediator.Send(
                new GenerateTaxCalendarCommand(year, tenantId), ct);
            return Results.Ok(new { year, eventsCreated = count });
        })
        .WithName("GenerateTaxCalendarFromReports")
        .WithSummary("Vergi takvimi olustur (yillik ~40 etkinlik)");

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
        .WithSummary("Platform bazli satis karsilastirma raporu (tarih araligi + platform filtresi)");

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
        .WithSummary("Karlilik raporu — Net Kar = Gelir - Alis - Komisyon - Kargo - KDV");
    }
}
