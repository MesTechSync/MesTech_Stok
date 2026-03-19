using MediatR;
using MesTech.Application.Commands.AddStock;
using MesTech.Application.Commands.RemoveStock;
using MesTech.Application.Queries.GetInventoryPaged;
using MesTech.Application.Queries.GetInventoryStatistics;
using MesTech.Application.Queries.GetInventoryValue;
using MesTech.Application.Queries.GetLowStockProducts;
using MesTech.Application.Queries.GetStockMovements;

namespace MesTech.WebApi.Endpoints;

public static class StockEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/stock").WithTags("Stock").RequireRateLimiting("PerApiKey");

        // GET /api/v1/stock/movements — list stock movements (optional filters)
        group.MapGet("/movements", async (
            Guid? productId,
            DateTime? from,
            DateTime? to,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetStockMovementsQuery(productId, from, to), ct);
            return Results.Ok(result);
        });

        // GET /api/v1/stock/value — total inventory value
        group.MapGet("/value", async (ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetInventoryValueQuery(), ct);
            return Results.Ok(result);
        });

        // POST /api/v1/stock/add — add stock to a product
        group.MapPost("/add", async (AddStockCommand command, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.BadRequest(new { result.ErrorMessage });
        });

        // POST /api/v1/stock/remove — remove stock from a product
        group.MapPost("/remove", async (RemoveStockCommand command, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.BadRequest(new { result.ErrorMessage });
        });

        // GET /api/v1/stock/inventory — paged inventory list with filters
        group.MapGet("/inventory", async (
            int? page,
            int? pageSize,
            string? search,
            StockStatusFilter? stockFilter,
            InventorySortOrder? sortOrder,
            ISender mediator,
            CancellationToken ct) =>
        {
            var query = new GetInventoryPagedQuery(
                Page: page ?? 1,
                PageSize: pageSize ?? 50,
                SearchTerm: search,
                StatusFilter: stockFilter ?? StockStatusFilter.All,
                SortOrder: sortOrder ?? InventorySortOrder.ProductName);
            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetInventoryPaged")
        .WithSummary("Sayfalanmış envanter listesi (arama + stok durumu filtresi)");

        // GET /api/v1/stock/low — products below minimum stock threshold
        group.MapGet("/low", async (ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetLowStockProductsQuery(), ct);
            return Results.Ok(result);
        })
        .WithName("GetLowStockProducts")
        .WithSummary("Minimum stok seviyesinin altındaki ürünler");

        // GET /api/v1/stock/statistics — inventory statistics (totals, values, alerts)
        group.MapGet("/statistics", async (ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetInventoryStatisticsQuery(), ct);
            return Results.Ok(result);
        })
        .WithName("GetInventoryStatistics")
        .WithSummary("Stok istatistikleri (toplam, değer, uyarılar)");

        // POST /api/v1/stock/transfer — inter-warehouse stock transfer
        // DEV1-DEPENDENCY: TransferStockCommand not yet implemented (empty directory exists)
        group.MapPost("/transfer", (HttpRequest request) =>
            Results.Ok(new
            {
                Message = "Stock transfer endpoint — DEV1 TransferStockCommand pending",
                Status = "not_implemented"
            }))
        .WithName("TransferStock")
        .WithSummary("Depolar arası stok transferi (DEV1-DEPENDENCY)");

        // POST /api/v1/stock/adjust — stock adjustment (correction/reconciliation)
        // DEV1-DEPENDENCY: AdjustStockCommand not yet implemented (empty directory exists)
        group.MapPost("/adjust", (HttpRequest request) =>
            Results.Ok(new
            {
                Message = "Stock adjustment endpoint — DEV1 AdjustStockCommand pending",
                Status = "not_implemented"
            }))
        .WithName("AdjustStock")
        .WithSummary("Stok düzeltme / sayım farkı girişi (DEV1-DEPENDENCY)");

        // POST /api/v1/stock/lot — add stock lot (batch tracking)
        // DEV1-DEPENDENCY: AddStockLotCommand does not exist yet
        group.MapPost("/lot", (HttpRequest request) =>
            Results.Ok(new
            {
                Message = "Stock lot endpoint — DEV1 AddStockLotCommand pending",
                Status = "not_implemented"
            }))
        .WithName("AddStockLot")
        .WithSummary("Lot/parti bazlı stok girişi (DEV1-DEPENDENCY)");
    }
}
