using MediatR;
using MesTech.Application.Features.Shipping.Commands.AutoShipOrder;
using MesTech.Application.Features.Shipping.Commands.BatchShipOrders;
using MesTech.Application.Features.Shipping.Queries.GetShipmentStatus;
using MesTech.Domain.Enums;

namespace MesTech.WebApi.Endpoints;

public static class ShippingEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/shipping")
            .WithTags("Shipping")
            .RequireRateLimiting("PerApiKey");

        // POST /api/v1/shipping/auto-ship — otomatik kargolama
        group.MapPost("/auto-ship", async (
            AutoShipOrderCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("AutoShipOrder")
        .WithSummary("Siparişi otomatik kargola (kargo firması seçimi dahil)");

        // POST /api/v1/shipping/batch-ship — toplu kargolama
        group.MapPost("/batch-ship", async (
            BatchShipOrdersCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("BatchShipOrders")
        .WithSummary("Birden fazla siparişi toplu kargola");

        // GET /api/v1/shipping/{trackingNumber}/status — kargo takip durumu
        group.MapGet("/{trackingNumber}/status", async (
            string trackingNumber, Guid tenantId, CargoProvider provider,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetShipmentStatusQuery(tenantId, trackingNumber, provider), ct);
            return Results.Ok(result);
        })
        .WithName("GetShipmentStatus")
        .WithSummary("Kargo takip numarasıyla gönderi durumu sorgula");
    }
}
