using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Features.Reporting.Commands.CreateSavedReport;
using MesTech.Application.Features.Reporting.Commands.DeleteSavedReport;
using MesTech.Application.Features.Reporting.Queries.GetSavedReports;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class SavedReportEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/reports/saved")
            .WithTags("SavedReports")
            .RequireRateLimiting("PerApiKey");

        // POST /api/v1/reports/saved — yeni kaydedilmis rapor olustur
        group.MapPost("/", async (
            CreateSavedReportCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/reports/saved/{id}", new CreatedResponse(id));
        })
        .WithName("CreateSavedReport")
        .WithSummary("Yeni kaydedilmis rapor sablonu olusturur");

        // GET /api/v1/reports/saved — tenant'a ait raporlari listele
        group.MapGet("/", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetSavedReportsQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetSavedReports")
        .WithSummary("Tenant'a ait kaydedilmis raporlari listeler");

        // DELETE /api/v1/reports/saved/{id} — kaydedilmis raporu sil
        group.MapDelete("/{id:guid}", async (
            Guid id, Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var deleted = await mediator.Send(
                new DeleteSavedReportCommand(tenantId, id), ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteSavedReport")
        .WithSummary("Kaydedilmis raporu siler");
    }
}
