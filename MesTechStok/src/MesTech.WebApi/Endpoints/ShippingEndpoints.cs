using MediatR;
using MesTech.Application.DTOs.Reports;
using MesTech.Application.DTOs.Shipping;
using MesTech.Application.Features.Reports.CargoPerformanceReport;
using MesTech.Application.Features.Shipping.Commands.AutoShipOrder;
using MesTech.Application.Features.Shipping.Commands.BatchShipOrders;
using MesTech.Application.Features.Shipping.Queries.GetShipmentStatus;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Microsoft.AspNetCore.OutputCaching;

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
        .WithSummary("Siparişi otomatik kargola (kargo firması seçimi dahil)").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/shipping/batch-ship — toplu kargolama
        group.MapPost("/batch-ship", async (
            BatchShipOrdersCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("BatchShipOrders")
        .WithSummary("Birden fazla siparişi toplu kargola").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/shipping/{trackingNumber}/status — kargo takip durumu
        group.MapGet("/{trackingNumber}/status", async (
            string trackingNumber, Guid tenantId, CargoProvider provider,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetShipmentStatusQuery(tenantId, trackingNumber, provider), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetShipmentStatus")
        .WithSummary("Kargo takip numarasıyla gönderi durumu sorgula").Produces<ShipmentStatusDto>(200).Produces(400);

        // GET /api/v1/shipping/comparison — kargo firma karşılaştırma (son 90 gün)
        group.MapGet("/comparison", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-90);
            var result = await mediator.Send(
                new CargoPerformanceReportQuery(tenantId, startDate, endDate), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetShippingPerformanceComparison")
        .WithSummary("Kargo firma karşılaştırma raporu (son 90 gün performans)").Produces<IReadOnlyList<CargoPerformanceReportDto>>(200).Produces(400);

        // GET /api/v1/shipping/track/{trackingNumber} — basit kargo takip (tüm sağlayıcıları dener)
        group.MapGet("/track/{trackingNumber}", async (
            string trackingNumber,
            Guid tenantId,
            ICargoProviderFactory cargoProviderFactory,
            ISender mediator, CancellationToken ct) =>
        {
            var allAdapters = cargoProviderFactory.GetAll();
            foreach (var adapter in allAdapters)
            {
                try
                {
                    var result = await mediator.Send(
                        new GetShipmentStatusQuery(tenantId, trackingNumber, adapter.Provider), ct);
                    if (result is not null && result.Events.Count > 0)
                        return Results.Ok(result);
                }
                catch (InvalidOperationException)
                {
                    // Provider not available, try next
                }
            }

            return Results.Problem(detail: $"Tracking number '{trackingNumber}' not found in any carrier.", statusCode: 404);
        })
        .CacheOutput("Lookup60s")
        .WithName("TrackShipment")
        .WithSummary("Kargo takip numarasıyla gönderi ara (tüm sağlayıcıları dener)").Produces<ShipmentStatusDto>(200).Produces(400);
    }
}
