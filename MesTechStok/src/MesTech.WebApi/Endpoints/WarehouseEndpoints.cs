using MediatR;
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
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetWarehousesQuery(activeOnly ?? true), ct);
            return Results.Ok(result);
        })
        .WithName("GetWarehouses")
        .WithSummary("Depo listesi (aktif/tümü filtresi)");

        // GET /api/v1/warehouses/{id} — depo detayı
        // DEV1-DEPENDENCY: GetWarehouseByIdQuery not yet available
        group.MapGet("/{id:guid}", (Guid id, CancellationToken ct) =>
            Results.Ok(new
            {
                Message = "Warehouse detail endpoint — DEV1 GetWarehouseByIdQuery pending",
                WarehouseId = id,
                Status = "not_implemented"
            }))
        .WithName("GetWarehouseById")
        .WithSummary("Depo detayı (DEV1-DEPENDENCY)");

        // POST /api/v1/warehouses — yeni depo oluştur
        // DEV1-DEPENDENCY: CreateWarehouseCommand not yet available
        group.MapPost("/", (HttpRequest request, CancellationToken ct) =>
            Results.Accepted("/api/v1/warehouses", new
            {
                Message = "Create warehouse endpoint — DEV1 CreateWarehouseCommand pending",
                Status = "not_implemented"
            }))
        .WithName("CreateWarehouse")
        .WithSummary("Yeni depo oluştur (DEV1-DEPENDENCY)");

        // PUT /api/v1/warehouses/{id} — depo güncelle
        // DEV1-DEPENDENCY: UpdateWarehouseCommand not yet available
        group.MapPut("/{id:guid}", (Guid id, HttpRequest request, CancellationToken ct) =>
            Results.Ok(new
            {
                Message = "Update warehouse endpoint — DEV1 UpdateWarehouseCommand pending",
                WarehouseId = id,
                Status = "not_implemented"
            }))
        .WithName("UpdateWarehouse")
        .WithSummary("Depo bilgilerini güncelle (DEV1-DEPENDENCY)");

        // DELETE /api/v1/warehouses/{id} — depo sil (soft-delete)
        // DEV1-DEPENDENCY: DeleteWarehouseCommand not yet available
        group.MapDelete("/{id:guid}", (Guid id, CancellationToken ct) =>
            Results.Ok(new
            {
                Message = "Delete warehouse endpoint — DEV1 DeleteWarehouseCommand pending",
                WarehouseId = id,
                Status = "not_implemented"
            }))
        .WithName("DeleteWarehouse")
        .WithSummary("Depo sil / pasife al (DEV1-DEPENDENCY)");
    }
}
