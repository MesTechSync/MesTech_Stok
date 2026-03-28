using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Features.Dropshipping.Commands;
using MesTech.Application.Features.Dropshipping.Queries;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class SupplierFeedsEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/supplier-feeds")
            .WithTags("SupplierFeeds")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/supplier-feeds — paginated feed source list
        group.MapGet("/", async (
            ISender mediator,
            bool? isActive,
            int page,
            int pageSize,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetFeedSourcesQuery(isActive, page <= 0 ? 1 : page, Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 100)), ct);
            return Results.Ok(result);
        })
        .WithName("GetSupplierFeeds")
        .WithSummary("Tedarikçi feed kaynakları listesi")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/supplier-feeds/stats — dashboard istatistikleri
        group.MapGet("/stats", async (ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPoolDashboardStatsQuery(), ct);
            return Results.Ok(result);
        })
        .WithName("GetSupplierFeedStats")
        .WithSummary("Havuz istatistikleri (toplam, renk dağılımı, son sync)")
        .Produces(200)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/supplier-feeds/{id} — tek feed kaynağı
        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetFeedSourceByIdQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetSupplierFeedById")
        .WithSummary("Tedarikçi feed kaynağını ID ile getir")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/supplier-feeds/{id}/logs — import geçmişi
        group.MapGet("/{id:guid}/logs", async (
            Guid id,
            ISender mediator,
            int page,
            int pageSize,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetFeedImportHistoryQuery(id, page <= 0 ? 1 : page, Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 100)), ct);
            return Results.Ok(result);
        })
        .WithName("GetFeedImportLogs")
        .WithSummary("Feed import geçmişi ve log kayıtları")
        .Produces(200)
        .CacheOutput("Report120s");

        // POST /api/v1/supplier-feeds — yeni feed kaynağı oluştur
        group.MapPost("/", async (CreateFeedSourceCommand command, ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/supplier-feeds/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("CreateSupplierFeed")
        .WithSummary("Yeni tedarikçi feed kaynağı oluştur").Produces(201).Produces(400);

        // PUT /api/v1/supplier-feeds/{id} — feed kaynağını güncelle
        group.MapPut("/{id:guid}", async (Guid id, UpdateFeedSourceCommand command, ISender mediator, CancellationToken ct) =>
        {
            if (id != command.Id)
                return Results.Problem(detail: "Route ID ile body ID uyuşmuyor.", statusCode: 400);

            await mediator.Send(command, ct);
            return Results.NoContent();
        })
        .WithName("UpdateSupplierFeed")
        .WithSummary("Tedarikçi feed kaynağını güncelle").Produces(200).Produces(400);

        // DELETE /api/v1/supplier-feeds/{id} — feed kaynağını sil (soft-delete)
        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteFeedSourceCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("DeleteSupplierFeed")
        .WithSummary("Tedarikçi feed kaynağını sil (soft-delete)").Produces(200).Produces(400);

        // POST /api/v1/supplier-feeds/{id}/sync — feed sync tetikle
        group.MapPost("/{id:guid}/sync", async (Guid id, ISender mediator, CancellationToken ct) =>
        {
            var jobId = await mediator.Send(new TriggerFeedImportCommand(id), ct);
            return Results.Accepted($"/api/v1/supplier-feeds/{id}/logs",
                new { jobId, message = "Sync kuyruğa alındı." });
        })
        .WithName("TriggerFeedSync")
        .WithSummary("Feed import işlemini arka planda tetikle").Produces(200).Produces(400);
    }
}
