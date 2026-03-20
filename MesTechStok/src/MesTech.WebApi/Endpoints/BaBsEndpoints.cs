using MediatR;
using MesTech.Application.Features.Accounting.Queries.GenerateBaBsReport;

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
        .WithSummary("Ba/Bs beyanname raporu (VUK 396 — 5.000 TL ustu alis/satis)");

        // POST /api/v1/accounting/babs-records — yeni Ba/Bs kaydi olustur
        // Awaiting DEV-1 CreateBaBsRecordCommand handler
        group.MapPost("/records", async (
            // CreateBaBsRecordCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            // var id = await mediator.Send(command, ct);
            // return Results.Created($"/api/v1/accounting/babs-records/{id}", new { id });
            return Results.StatusCode(StatusCodes.Status501NotImplemented);
        })
        .WithName("CreateBaBsRecord")
        .WithSummary("Yeni Ba/Bs kaydi olustur (DEV-1 handler bekleniyor)");
    }
}
