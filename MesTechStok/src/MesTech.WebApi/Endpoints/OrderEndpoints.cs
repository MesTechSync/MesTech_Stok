using MediatR;
using MesTech.Application.Commands.PlaceOrder;
using MesTech.Application.Queries.ListOrders;

namespace MesTech.WebApi.Endpoints;

public static class OrderEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/orders").WithTags("Orders").RequireRateLimiting("PerApiKey");

        // GET /api/v1/orders — list orders (optional date range + status filter)
        group.MapGet("/", async (
            DateTime? from,
            DateTime? to,
            string? status,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ListOrdersQuery(from, to, status), ct);
            return Results.Ok(result);
        })
        .WithName("ListOrders")
        .WithSummary("Sipariş listesi (tarih + durum filtresi)");

        // POST /api/v1/orders — yeni sipariş oluştur
        group.MapPost("/", async (
            PlaceOrderCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/orders/{result.OrderId}", new { result.OrderId })
                : Results.BadRequest(new { result.ErrorMessage });
        })
        .WithName("PlaceOrder")
        .WithSummary("Yeni sipariş oluştur");
    }
}
