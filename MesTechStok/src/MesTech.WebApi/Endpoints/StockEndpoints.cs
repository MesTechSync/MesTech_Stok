using MediatR;
using MesTech.Application.Commands.AddStock;
using MesTech.Application.Commands.RemoveStock;
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
    }
}
