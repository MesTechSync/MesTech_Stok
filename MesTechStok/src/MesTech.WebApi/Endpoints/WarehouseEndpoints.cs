using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Commands.CreateWarehouse;
using MesTech.Application.Commands.DeleteWarehouse;
using MesTech.Application.Commands.UpdateWarehouse;
using MesTech.Application.Queries.GetWarehouseById;
using MesTech.Application.Queries.GetWarehouses;
using MesTech.Application.Queries.GetWarehouseStock;
using MesTech.Application.Queries.GetWarehouseSummary;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class WarehouseEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/warehouses")
            .WithTags("Warehouses")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/warehouses — depo listesi
        group.MapGet("/", async (
            bool? activeOnly,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetWarehousesQuery(activeOnly ?? true), ct);
            return Results.Ok(result);
        })
        .WithName("GetWarehouses")
        .WithSummary("Depo listesi (aktif/tümü filtresi)")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/warehouses/{id} — depo detayı
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetWarehouseByIdQuery(id), ct);
            return result is not null
                ? Results.Ok(result)
                : Results.NotFound(new { error = $"Warehouse {id} not found" });
        })
        .WithName("GetWarehouseById")
        .WithSummary("Depo detayı")
        .Produces(200)
        .Produces(404)
        .CacheOutput("Lookup60s");

        // POST /api/v1/warehouses — yeni depo oluştur
        group.MapPost("/", async (
            CreateWarehouseCommand command,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/warehouses/{result.WarehouseId}", new CreatedResponse(result.WarehouseId))
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("CreateWarehouse")
        .WithSummary("Yeni depo oluştur")
        .Produces(201)
        .Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // PUT /api/v1/warehouses/{id} — depo güncelle
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateWarehouseRequest request,
            ISender sender, CancellationToken ct) =>
        {
            var success = await sender.Send(
                new UpdateWarehouseCommand(
                    request.TenantId, id, request.Name, request.Code,
                    request.Description, request.Type, request.IsActive), ct);
            return success
                ? Results.NoContent()
                : Results.NotFound(new { error = $"Warehouse {id} not found or update failed" });
        })
        .WithName("UpdateWarehouse")
        .WithSummary("Depo bilgilerini güncelle")
        .Produces(204)
        .Produces(404)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // DELETE /api/v1/warehouses/{id} — depo sil (soft-delete)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            Guid tenantId,
            ISender sender, CancellationToken ct) =>
        {
            var success = await sender.Send(
                new DeleteWarehouseCommand(tenantId, id), ct);
            return success
                ? Results.NoContent()
                : Results.NotFound(new { error = $"Warehouse {id} not found or delete failed" });
        })
        .WithName("DeleteWarehouse")
        .WithSummary("Depo sil / pasife al")
        .Produces(204)
        .Produces(404);

        // GET /api/v1/warehouses/{id}/stock — depo stok listesi
        group.MapGet("/{id:guid}/stock", async (
            Guid id, Guid tenantId,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetWarehouseStockQuery(id, tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetWarehouseStock")
        .WithSummary("Depo bazlı stok listesi")
        .Produces(200)
        .CacheOutput("Report120s");

        // GET /api/v1/warehouses/summary — depo özet raporu
        group.MapGet("/summary", async (
            Guid tenantId,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetWarehouseSummaryQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetWarehouseSummary")
        .WithSummary("Tüm depoların özet raporu")
        .Produces(200)
        .CacheOutput("Dashboard30s");
    }

    private record UpdateWarehouseRequest(
        Guid TenantId, string Name, string Code,
        string? Description, string Type, bool IsActive);
}
