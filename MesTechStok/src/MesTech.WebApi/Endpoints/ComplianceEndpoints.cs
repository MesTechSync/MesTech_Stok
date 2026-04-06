using MesTech.Application.DTOs;
using MediatR;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Yasal uyumluluk endpoint'leri — ETBİS, iade faturası, aylık kapanış.
/// E10: ETBİS yıllık raporlama (Ticaret Bakanlığı zorunlu)
/// E25: İade faturası (credit note) oluşturma
/// E50: Aylık muhasebe kapanış (KDV beyan + Ba/Bs + trial balance)
/// </summary>
public static class ComplianceEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/compliance")
            .WithTags("Compliance & Legal")
            .RequireRateLimiting("PerApiKey");

        // ═══ E10: ETBİS RAPORLAMA ═══

        // GET /api/v1/compliance/etbis/{year} — ETBİS yıllık rapor verisi
        group.MapGet("/etbis/{year:int}", async (
            int year, Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var platformReports = await mediator.Send(
                new Application.Features.Reports.PlatformSalesReport.PlatformSalesReportQuery(
                    tenantId, new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc)), ct);

            var totalRevenue = platformReports.Sum(p => p.TotalRevenue);
            var totalOrders = platformReports.Sum(p => p.TotalOrders);
            var totalReturns = platformReports.Sum(p => p.Returns);
            var breakdown = platformReports.ToDictionary(p => p.Platform, p => p.TotalRevenue);

            var result = new EtbisReportResponse(
                Year: year,
                TenantId: tenantId,
                TotalRevenue: totalRevenue,
                TotalOrders: totalOrders,
                TotalReturns: totalReturns,
                PlatformCount: platformReports.Count,
                PlatformBreakdown: breakdown,
                GeneratedAt: DateTime.UtcNow,
                Disclaimer: "Bu veriler ETBİS beyannamesi için ön bilgi niteliğindedir. " +
                            "Kesin beyanname etbis.eticaret.gov.tr üzerinden yapılmalıdır.");

            return Results.Ok(result);
        })
        .WithName("GetEtbisReport")
        .WithSummary("ETBİS yıllık rapor verisi (Ticaret Bakanlığı — etbis.eticaret.gov.tr)")
        .Produces<EtbisReportResponse>(200)
        .CacheOutput("Report120s");

        // ═══ E25: İADE FATURASI (CREDIT NOTE) ═══

        // POST /api/v1/compliance/credit-note — iade faturası oluştur
        // BAĞIMLILIK: DEV1 → CreateCreditNoteCommand handler gerekli
        // Bu endpoint handler yazıldığında aktif edilecek.
        group.MapPost("/credit-note", (
            CreditNoteRequest request,
            ISender mediator,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("ComplianceEndpoints");
            logger.LogWarning(
                "[CreditNote] İade faturası talebi: OriginalInvoice={InvoiceId}, Amount={Amount:N2}",
                request.OriginalInvoiceId, request.Amount);

            // İade faturası = orijinal faturanın tersine kaydı
            // DEV1 handler yazıldığında:
            // var invoiceId = await mediator.Send(new CreateCreditNoteCommand(...), ct);
            // return Results.Created($"/api/v1/invoices/{invoiceId}", ...);

            return Results.Problem(
                detail: "İade faturası (credit note) handler henüz implementsız. DEV1 görevi: CreateCreditNoteCommand.",
                statusCode: 501);
        })
        .WithName("CreateCreditNote")
        .WithSummary("İade faturası (credit note) oluştur — iade onayı sonrası GİB'e gönderilir")
        .Produces(201).Produces(400).Produces(501)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // ═══ E50: AYLIK MUHASEBE KAPANIŞ ═══

        // POST /api/v1/compliance/monthly-close — aylık kapanış başlat
        group.MapPost("/monthly-close", async (
            MonthlyCloseRequest request,
            ISender mediator,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("ComplianceEndpoints");

            logger.LogInformation(
                "[MonthlyClose] Aylık kapanış başlatıldı: {Year}-{Month:D2}, tenant={TenantId}",
                request.Year, request.Month, request.TenantId);

            // 1. Trial Balance doğrulama
            var trialBalance = await mediator.Send(
                new Application.Features.Accounting.Queries.ValidateTrialBalance.ValidateTrialBalanceQuery(
                    request.TenantId,
                    new DateTime(request.Year, request.Month, 1),
                    new DateTime(request.Year, request.Month, 1).AddMonths(1).AddSeconds(-1)), ct);

            // 2. KDV raporu hazırla
            var kdv = await mediator.Send(
                new Application.Features.Accounting.Queries.GetKdvReport.GetKdvReportQuery(
                    request.TenantId, request.Year, request.Month), ct);

            // 3. Ba/Bs rapor hazırla
            var babs = await mediator.Send(
                new Application.Features.Accounting.Queries.GenerateBaBsReport.GenerateBaBsReportQuery(
                    request.TenantId, request.Year, request.Month), ct);

            var result = new MonthlyCloseResponse(
                Period: $"{request.Year}-{request.Month:D2}",
                TrialBalanceValid: trialBalance?.IsBalanced ?? false,
                KdvPayable: kdv?.OdenecekKdv ?? 0,
                BaBsRecordCount: (babs?.BaEntries.Count + babs?.BsEntries.Count) ?? 0,
                ClosedAt: DateTime.UtcNow,
                Status: (trialBalance?.IsBalanced ?? false) ? "READY" : "IMBALANCED",
                Message: (trialBalance?.IsBalanced ?? false)
                    ? "Mizan dengeli, aylık kapanış hazır."
                    : "⚠️ Mizan dengesiz! Düzeltme yevmiyesi gerekli.");

            logger.LogInformation(
                "[MonthlyClose] Tamamlandı: {Period} — Mizan={Balanced}, KDV={Kdv:N2}, BaBs={BaBs}",
                result.Period, result.TrialBalanceValid, result.KdvPayable, result.BaBsRecordCount);

            return Results.Ok(result);
        })
        .WithName("ExecuteMonthlyClose")
        .WithSummary("Aylık muhasebe kapanış — mizan doğrulama + KDV + Ba/Bs birleşik rapor")
        .Produces<MonthlyCloseResponse>(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();
    }

    // ── Request/Response DTOs ──

    public sealed record EtbisReportResponse(
        int Year, Guid TenantId,
        decimal TotalRevenue, int TotalOrders, int TotalReturns,
        int PlatformCount,
        Dictionary<string, decimal> PlatformBreakdown,
        DateTime GeneratedAt, string Disclaimer);

    public sealed record CreditNoteRequest(
        Guid TenantId, Guid OriginalInvoiceId, Guid? ReturnId,
        string Reason, decimal Amount);

    public sealed record MonthlyCloseRequest(
        Guid TenantId, int Year, int Month);

    public sealed record MonthlyCloseResponse(
        string Period, bool TrialBalanceValid,
        decimal KdvPayable, int BaBsRecordCount,
        DateTime ClosedAt, string Status, string Message);
}
