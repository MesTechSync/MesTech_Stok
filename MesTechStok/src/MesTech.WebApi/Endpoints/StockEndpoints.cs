using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using MesTech.Application.Commands.AddStock;
using MesTech.Application.Commands.AddStockLot;
using MesTech.Application.Commands.AdjustStock;
using MesTech.Application.Commands.RemoveStock;
using MesTech.Application.Commands.TransferStock;
using MesTech.Application.Queries.GetInventoryPaged;
using MesTech.Application.Queries.GetInventoryStatistics;
using MesTech.Application.Queries.GetInventoryValue;
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
        })
        .WithName("GetStockMovements")
        .WithSummary("Stok hareketleri listesi (ürün, tarih filtresi)")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/stock/value — total inventory value
        group.MapGet("/value", async (ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetInventoryValueQuery(), ct);
            return Results.Ok(result);
        })
        .WithName("GetInventoryValue")
        .WithSummary("Toplam envanter değeri")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // POST /api/v1/stock/add — add stock to a product
        group.MapPost("/add", async (AddStockCommand command, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("AddStock")
        .WithSummary("Ürüne stok girişi")
        .Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/stock/remove — remove stock from a product
        group.MapPost("/remove", async (RemoveStockCommand command, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("RemoveStock")
        .WithSummary("Üründen stok çıkışı")
        .Produces(200).Produces(400);

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
        .WithSummary("Sayfalanmış envanter listesi (arama + stok durumu filtresi)")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/stock/statistics — inventory statistics (totals, values, alerts)
        group.MapGet("/statistics", async (ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetInventoryStatisticsQuery(), ct);
            return Results.Ok(result);
        })
        .WithName("GetInventoryStatistics")
        .WithSummary("Stok istatistikleri (toplam, değer, uyarılar)")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // POST /api/v1/stock/transfer — inter-warehouse stock transfer
        group.MapPost("/transfer", async (
            TransferStockCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("TransferStock")
        .WithSummary("Depolar arası stok transferi")
        .Produces(200).Produces(400);

        // POST /api/v1/stock/adjust — stock adjustment (correction/reconciliation)
        group.MapPost("/adjust", async (
            AdjustStockCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("AdjustStock")
        .WithSummary("Stok düzeltme / sayım farkı girişi")
        .Produces(200).Produces(400);

        // POST /api/v1/stock/lot — add stock lot (batch tracking)
        group.MapPost("/lot", async (
            AddStockLotCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("AddStockLot")
        .WithSummary("Lot/parti bazlı stok girişi")
        .Produces(200).Produces(400);
    }
}
