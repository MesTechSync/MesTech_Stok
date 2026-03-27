using MesTech.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using MesTech.Application.Features.Accounting.Commands.CreateBaBsRecord;
using MesTech.Application.Features.Accounting.Queries.GenerateBaBsReport;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.WebApi.Endpoints;

public static class BaBsEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/accounting/babs-report")
            .WithTags("Accounting - Ba/Bs")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/accounting/babs-report/{year}/{month} — Ba/Bs beyanname raporu
        group.MapGet("/{year:int}/{month:int}", async (
            int year, int month, Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GenerateBaBsReportQuery(tenantId, year, month), ct);
            return Results.Ok(result);
        })
        .WithName("GenerateBaBsReport")
        .WithSummary("Ba/Bs beyanname raporu (VUK 396 — 5.000 TL ustu alis/satis)")
        .Produces(200)
        .CacheOutput("Report120s");

        // POST /api/v1/accounting/babs-records — yeni Ba/Bs kaydi olustur
        group.MapPost("/records", async (
            CreateBaBsRecordCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/accounting/babs-records/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("CreateBaBsRecord")
        .WithSummary("Yeni Ba/Bs kaydi olustur (VUK 396)").Produces(200).Produces(400);

        // ─── V5 YENİ BA/BS ENDPOINT'LERİ [ENT-DEV6] ───

        // GET /api/v1/accounting/babs-report/{year}/{month}/export/xml — GİB XML export
        group.MapGet("/{year:int}/{month:int}/export/xml", async (
            int year, int month, Guid tenantId,
            string formType,
            string tenantVKN,
            string tenantName,
            ISender mediator,
            IBaBsXmlExportService xmlExport,
            CancellationToken ct) =>
        {
            if (formType is not ("Ba" or "Bs"))
                return Results.BadRequest(new { error = "formType 'Ba' veya 'Bs' olmalı" });

            if (month < 1 || month > 12)
                return Results.BadRequest(new { error = "month 1-12 arasında olmalı" });

            var report = await mediator.Send(
                new GenerateBaBsReportQuery(tenantId, year, month), ct);

            var xmlBytes = await xmlExport.ExportToXmlAsync(
                report, formType, year, month, tenantVKN, tenantName);

            var fileName = $"{formType}_Form_{year}_{month:D2}.xml";
            return Results.File(xmlBytes, "application/xml", fileName);
        })
        .WithName("ExportBaBsXml")
        .WithSummary("Ba/Bs GİB XML dosyası export — VUK 396 formatında").Produces(200).Produces(400);

        // GET /api/v1/accounting/babs-report/deadline — sonraki beyanname son tarihi
        group.MapGet("/deadline", (int? year, int? month) =>
        {
            var now = DateTime.UtcNow;
            var targetYear = year ?? now.Year;
            var targetMonth = month ?? now.Month;

            // VUK 396: Ba/Bs beyanname son günü = takip eden ayın son günü
            var nextMonth = new DateTime(targetYear, targetMonth, 1).AddMonths(1);
            var deadline = new DateTime(nextMonth.Year, nextMonth.Month,
                DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month), 23, 59, 59);

            var remaining = deadline - now;
            var isOverdue = remaining.TotalSeconds < 0;

            return Results.Ok(new
            {
                period = $"{targetYear}-{targetMonth:D2}",
                deadline = deadline.ToString("yyyy-MM-dd HH:mm:ss"),
                remainingDays = isOverdue ? 0 : (int)remaining.TotalDays,
                isOverdue,
                threshold = 5000m,
                description = "VUK 396 — Ba/Bs beyanname son günü: takip eden ayın son günü"
            });
        })
        .WithName("GetBaBsDeadline")
        .WithSummary("Ba/Bs beyanname son tarihi hesapla (VUK 396)")
        .Produces(200)
        .CacheOutput("Lookup60s");
    }
}
