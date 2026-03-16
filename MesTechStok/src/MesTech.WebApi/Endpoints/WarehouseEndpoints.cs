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
        group.MapGet("/{id:guid}", (Guid id) =>
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
        group.MapPost("/", () =>
            Results.Accepted("/api/v1/warehouses", new
            {
                Message = "Create warehouse endpoint — DEV1 CreateWarehouseCommand pending",
                Status = "not_implemented"
            }))
        .WithName("CreateWarehouse")
        .WithSummary("Yeni depo oluştur (DEV1-DEPENDENCY)");
    }
}
