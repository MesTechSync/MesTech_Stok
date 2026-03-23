using MediatR;
using MesTech.Application.Commands.CreateWarehouse;
using MesTech.Application.Commands.DeleteWarehouse;
using MesTech.Application.Commands.UpdateWarehouse;
using MesTech.Application.Queries.GetWarehouses;

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
        .WithSummary("Depo listesi (aktif/tümü filtresi)");

        // GET /api/v1/warehouses/{id} — depo detayı
        // TODO: GetWarehouseByIdQuery handler not yet available
        group.MapGet("/{id:guid}", (Guid id, CancellationToken ct) =>
            Results.Ok(new
            {
                Message = "Warehouse detail endpoint — GetWarehouseByIdQuery handler not yet available",
                WarehouseId = id,
                Status = "not_implemented"
            }))
        .WithName("GetWarehouseById")
        .WithSummary("Depo detayı (TODO: GetWarehouseByIdQuery handler gerekli)");

        // POST /api/v1/warehouses — yeni depo oluştur
        group.MapPost("/", async (
            CreateWarehouseCommand command,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/warehouses/{result.WarehouseId}", new { id = result.WarehouseId })
                : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .WithName("CreateWarehouse")
        .WithSummary("Yeni depo oluştur");

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
        .WithSummary("Depo bilgilerini güncelle");

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
        .WithSummary("Depo sil / pasife al");
    }

    private record UpdateWarehouseRequest(
        Guid TenantId, string Name, string Code,
        string? Description, string Type, bool IsActive);
}
