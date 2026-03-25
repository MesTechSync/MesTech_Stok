using MediatR;
using MesTech.Application.Commands.PlaceOrder;
using MesTech.Application.Commands.PushOrderToBitrix24;
using MesTech.Application.Features.Orders.Queries.GetStaleOrders;
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
        .WithSummary("Sipariş listesi (tarih + durum filtresi)")
        .Produces(200);

        // POST /api/v1/orders — yeni sipariş oluştur
        group.MapPost("/", async (
            PlaceOrderCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/orders/{result.OrderId}", new { result.OrderId })
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("PlaceOrder")
        .WithSummary("Yeni sipariş oluştur")
        .Produces(201)
        .Produces(400);

        // POST /api/v1/orders/{id}/push-bitrix24 — siparişi Bitrix24 CRM'e gönder
        group.MapPost("/{id:guid}/push-bitrix24", async (
            Guid id,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new PushOrderToBitrix24Command(id), ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .WithName("PushOrderToBitrix24")
        .WithSummary("Siparişi Bitrix24 CRM'e deal olarak gönder")
        .Produces(200)
        .Produces(400);
    }
}
