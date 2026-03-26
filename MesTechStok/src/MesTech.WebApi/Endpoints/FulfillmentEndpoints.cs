using MediatR;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.Features.Fulfillment.Commands.CreateInboundShipment;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentInventory;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentDashboard;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentOrders;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentShipments;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class FulfillmentEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/fulfillment")
            .WithTags("Fulfillment")
            .RequireRateLimiting("PerApiKey");

        // POST /api/v1/fulfillment/inbound — create inbound shipment
        group.MapPost("/inbound", async (
            ISender mediator,
            CreateInboundShipmentCommand command,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/fulfillment/inbound", result);
        })
        .WithName("CreateInboundShipment")
        .WithSummary("Depo giriş sevkiyatı oluştur (FBA/Hepsilojistik)");

        // POST /api/v1/fulfillment/inventory — query fulfillment inventory
        group.MapPost("/inventory", async (
            ISender mediator,
            GetFulfillmentInventoryQuery query,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetFulfillmentInventory")
        .WithSummary("Fulfillment envanter sorgula (SKU bazlı)");

        // GET /api/v1/fulfillment/orders — fulfillment orders
        group.MapGet("/orders", async (
            ISender mediator,
            int center,
            DateTime since,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(
                new GetFulfillmentOrdersQuery((FulfillmentCenter)center, since), ct);
            return Results.Ok(result);
        })
        .WithName("GetFulfillmentOrders")
        .WithSummary("Fulfillment sipariş listesi")
        .CacheOutput("Lookup60s");

        // GET /api/v1/fulfillment/dashboard — fulfillment dashboard özeti
        group.MapGet("/dashboard", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetFulfillmentDashboardQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetFulfillmentDashboard")
        .WithSummary("Fulfillment dashboard — envanter, sevkiyat, bekleyen özeti")
        .CacheOutput("Dashboard30s");

        // GET /api/v1/fulfillment/shipments — sevkiyat listesi
        group.MapGet("/shipments", async (
            Guid tenantId, string? center, string? status, int page, int pageSize,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetFulfillmentShipmentsQuery(tenantId, center, status, page, pageSize), ct);
            return Results.Ok(result);
        })
        .WithName("GetFulfillmentShipments")
        .WithSummary("Fulfillment sevkiyat listesi (merkez + durum filtresi)")
        .CacheOutput("Lookup60s");
    }
}
