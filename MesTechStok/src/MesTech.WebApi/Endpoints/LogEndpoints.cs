using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Features.Logging.Commands.CleanOldLogs;
using MesTech.Application.Features.Logging.Commands.CreateLogEntry;
using MesTech.Application.Features.Logging.Queries.GetLogCount;
using MesTech.Application.Features.Logging.Queries.GetLogs;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class LogEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/logs")
            .WithTags("Logs")
            .RequireRateLimiting("PerApiKey");

        // POST /api/v1/logs — log kaydı oluştur
        group.MapPost("/", async (
            CreateLogEntryCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var logId = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/logs/{logId}", new CreatedResponse(logId));
        })
        .WithName("CreateLogEntry")
        .WithSummary("Yeni log kaydı oluştur").Produces(200).Produces(400);

        // GET /api/v1/logs — log listesi (sayfalanmış, filtreli)
        group.MapGet("/", async (
            Guid tenantId,
            int page = 1, int pageSize = 50,
            string? category = null, string? userId = null,
            string? productName = null, string? barcode = null,
            DateTime? startDate = null, DateTime? endDate = null,
            ISender mediator = default!, CancellationToken ct = default) =>
        {
            var result = await mediator.Send(
                new GetLogsQuery(tenantId, Math.Max(1, page), Math.Clamp(pageSize, 1, 100), category, userId, productName, barcode, startDate, endDate), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetLogs")
        .WithSummary("Log listesi (sayfalanmış, kategori/tarih/kullanıcı filtresi)").Produces(200).Produces(400);

        // GET /api/v1/logs/count — log sayısı
        group.MapGet("/count", async (
            Guid tenantId, string? category = null,
            ISender mediator = default!, CancellationToken ct = default) =>
        {
            var count = await mediator.Send(new GetLogCountQuery(tenantId, category), ct);
            return Results.Ok(new StatusResponse("Ok", $"{count}"));
        })
        .CacheOutput("Lookup60s")
        .WithName("GetLogCount")
        .WithSummary("Log kayıt sayısı (kategori filtresi)").Produces(200).Produces(400);

        // DELETE /api/v1/logs/clean — eski logları temizle
        group.MapDelete("/clean", async (
            Guid tenantId, int? daysToKeep,
            ISender mediator, CancellationToken ct) =>
        {
            var count = await mediator.Send(
                new CleanOldLogsCommand(tenantId, daysToKeep ?? 90), ct);
            return Results.Ok(new MesTech.Application.DTOs.DeletedCountResponse(count));
        })
        .WithName("CleanOldLogs")
        .WithSummary("Eski log kayıtlarını temizle (varsayılan: 90 gün)")
        .Produces(200);
    }
}
