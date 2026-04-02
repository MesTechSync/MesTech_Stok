using MesTech.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using MesTech.Application.Commands.CancelOrder;
using MesTech.Application.Commands.PlaceOrder;
using MesTech.Application.Commands.PushOrderToBitrix24;
using MesTech.Application.Commands.UpdateOrderStatus;
using MesTech.Application.Features.Orders.Commands.ExportOrders;
using MesTech.Application.Features.Orders.Queries.GetOrderDetail;
using MesTech.Application.Features.Orders.Queries.GetOrderList;
using MesTech.Application.Features.Orders.Queries.GetOrdersByStatus;
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
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Lookup60s");

        // POST /api/v1/orders — yeni sipariş oluştur
        group.MapPost("/", async (
            PlaceOrderCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/orders/{result.OrderId}", new CreatedResponse(result.OrderId))
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("PlaceOrder")
        .WithSummary("Yeni sipariş oluştur")
        .Produces(201).ProducesProblem(401).ProducesProblem(429)
        .Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/orders/{id}/push-bitrix24 — siparişi Bitrix24 CRM'e gönder
        group.MapPost("/{id:guid}/push-bitrix24", async (
            Guid id,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new PushOrderToBitrix24Command(id), ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("PushOrderToBitrix24")
        .WithSummary("Siparişi Bitrix24 CRM'e deal olarak gönder")
        .AddEndpointFilter<Filters.IdempotencyFilter>()
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .Produces(400).ProducesProblem(401).ProducesProblem(429);

        // GET /api/v1/orders/stale — gecikmiş sipariş listesi (menu #27 StaleOrders)
        group.MapGet("/stale", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetStaleOrdersQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetStaleOrders")
        .WithSummary("Gecikmiş siparişler — platform bazlı SLA aşımı")
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/orders/list — tenant-scoped order list (paged)
        group.MapGet("/list", async (
            Guid tenantId,
            int? count,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetOrderListQuery(tenantId, count ?? 100), ct);
            return Results.Ok(result);
        })
        .WithName("GetOrderList")
        .WithSummary("Tenant bazlı sipariş listesi (son N adet)")
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Lookup60s");

        // GET /api/v1/orders/by-status — kanban view: orders grouped by status
        group.MapGet("/by-status", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetOrdersByStatusQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetOrdersByStatus")
        .WithSummary("Sipariş kanban görünümü — duruma göre gruplu")
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/orders/{id} — sipariş detayı (P0 — DEV6 TUR10)
        group.MapGet("/{id:guid}", async (
            Guid id,
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetOrderDetailQuery(tenantId, id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetOrderDetail")
        .WithSummary("Sipariş detayı — satır kalemleri, kargo, müşteri bilgisi dahil")
        .Produces<OrderDetailDto>(200)
        .Produces(404).ProducesProblem(401).ProducesProblem(429);

        // POST /api/v1/orders/export — export orders to Excel
        group.MapPost("/export", async (
            ExportOrdersCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            if (!result.IsSuccess || result.FileContent is null)
                return Results.Problem(detail: result.ErrorMessage ?? "Export failed", statusCode: 400);
            return Results.File(result.FileContent?.ToArray() ?? Array.Empty<byte>(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                result.FileName);
        })
        .WithName("ExportOrders")
        .WithSummary("Siparişleri Excel'e aktar (tarih + platform filtresi)")
        .Produces(200).ProducesProblem(401).ProducesProblem(429)
        .Produces(400).ProducesProblem(401).ProducesProblem(429);

        // PUT /api/v1/orders/{id}/status — sipariş durumu güncelle (G528)
        group.MapPut("/{id:guid}/status", async (
            Guid id,
            UpdateOrderStatusCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            if (id != command.OrderId)
                return Results.BadRequest(ApiResponse<object>.Fail("Route ID and body OrderId mismatch"));

            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(new StatusResponse("updated", $"Order {id} status changed to {command.NewStatus}"))
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("UpdateOrderStatus")
        .WithSummary("Sipariş durumu güncelle — Place/Ship/Deliver/Complete akışı")
        .AddEndpointFilter<Filters.IdempotencyFilter>()
        .Produces<StatusResponse>(200)
        .Produces(400).ProducesProblem(401).ProducesProblem(429);

        // POST /api/v1/orders/{id}/cancel — sipariş iptal (G528)
        group.MapPost("/{id:guid}/cancel", async (
            Guid id,
            CancelOrderRequest? request,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new CancelOrderCommand(id, request?.Reason), ct);
            return result.IsSuccess
                ? Results.Ok(new StatusResponse("cancelled", $"Order {id} cancelled"))
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("CancelOrder")
        .WithSummary("Sipariş iptal et — iade süreci başlatır")
        .AddEndpointFilter<Filters.IdempotencyFilter>()
        .Produces<StatusResponse>(200)
        .Produces(400).ProducesProblem(401).ProducesProblem(429);
    }

    public record CancelOrderRequest(string? Reason);
}
